# LINQ Provider — Spec-Driven Development

## Context

This spec builds the LINQ provider layer over `TreePersistence.Core`. It depends
on the secondary indexes spec — the provider requires `IDocumentCollection<T>`,
`IDocumentIndexRegistry<T>`, and `IIndexExecutor<T>` to be in place.

Read the secondary indexes spec before this one.

---

## Agent Instructions

You are implementing a LINQ provider over a document storage engine in C# .NET 8.
- `IQueryable<T>` is the public surface — callers write standard LINQ
- The expression tree is translated into an internal query model before execution
- Index-aware planning must be applied at translation time using the registry
- All async terminal operators must use `IAsyncEnumerable` under the hood
- Implement strictly in the order specified
- Do not expose any storage engine types in the public LINQ API

---

## Changelog / What Is New

| Item | Description |
|---|---|
| `DocumentQueryable<T>` | `IQueryable<T>` implementation — the public query object |
| `DocumentQueryProvider<T>` | `IQueryProvider` — translates and executes expression trees |
| `QueryTranslator<T>` | Expression tree visitor — produces `TranslatedQuery<T>` |
| `TranslatedQuery<T>` | Structured internal query model |
| `FilterClause` hierarchy | `ComparisonFilter`, `AndFilter`, `OrFilter`, `NotFilter`, `MethodFilter` |
| `OrderClause` | Sort direction and member access |
| `MemberAccess` | Field name + resolved `IndexDefinition` |
| `QueryPlanner<T>` | Selects optimal execution plan from translated query |
| `ExecutionPlan<T>` hierarchy | `IndexScanPlan`, `IndexPointLookupPlan`, `FullScanPlan` |
| `IIndexExecutor<T>` | (from secondary indexes spec) — used here for plan execution |
| `DocumentQueryableExtensions` | `ToListAsync`, `FirstOrDefaultAsync`, `CountAsync`, `AnyAsync`, `ToAsyncEnumerable` |
| `PredicateAnalyzer` | Inspects a filter clause to determine index applicability |
| `ExpressionEvaluator` | Evaluates constant and closure expressions in expression trees |
| `NotSupportedInQueryException` | Thrown for unsupported LINQ operators or expressions |
| `QueryWarning` | Attached to full-scan queries — loggable diagnostic |
| `IQueryDiagnostics` | Optional sink for query plan diagnostics |

---

## Project Structure

```
TreePersistence.Core/
└── Linq/
    ├── DocumentQueryable.cs
    ├── DocumentQueryProvider.cs
    ├── Translation/
    │   ├── QueryTranslator.cs
    │   ├── PredicateAnalyzer.cs
    │   ├── ExpressionEvaluator.cs
    │   ├── FilterClause.cs
    │   ├── OrderClause.cs
    │   ├── MemberAccess.cs
    │   └── TranslatedQuery.cs
    ├── Planning/
    │   ├── QueryPlanner.cs
    │   ├── ExecutionPlan.cs
    │   └── IQueryDiagnostics.cs
    └── DocumentQueryableExtensions.cs

TreePersistence.Tests/
└── Linq/
    ├── QueryTranslatorTests.cs
    ├── QueryPlannerTests.cs
    ├── DocumentQueryableTests.cs
    ├── AsyncExtensionTests.cs
    └── IntegrationTests.cs
```

---

## How LINQ Providers Work — Agent Reference

When a caller writes:

```csharp
collection.AsQueryable()
          .Where(c => c.Age > 25)
          .OrderBy(c => c.LastName)
          .Take(10)
          .ToListAsync()
```

The C# compiler converts each lambda to an `Expression<Func<T,bool>>` — an in-memory
tree of `Expression` nodes. No lambda is executed. The chain of LINQ operators builds
a nested `MethodCallExpression` tree. The terminal operator (`ToListAsync`) triggers
the provider to walk the tree, translate it, plan it, and execute it.

The expression tree for `c => c.Age > 25` is:

```
BinaryExpression(GreaterThan)
├── MemberExpression(c.Age)
│   └── ParameterExpression(c : Customer)
└── ConstantExpression(25)
```

The full chain `.Where(...).OrderBy(...).Take(10)` produces:

```
MethodCallExpression(Take)
└── arg[0]: MethodCallExpression(OrderBy)
    └── arg[0]: MethodCallExpression(Where)
        └── arg[0]: ConstantExpression(DocumentQueryable<Customer>)  ← base
```

The translator visits this tree bottom-up, accumulating state.

---

## Layer 1 — Filter Clause Hierarchy

```csharp
namespace TreePersistence.Core.Linq.Translation;

/// <summary>
/// Represents a single predicate clause extracted from a Where expression.
/// Forms a tree matching the original boolean expression structure.
/// </summary>
public abstract record FilterClause
{
    /// <summary>
    /// True if this clause can be fully satisfied by an index scan.
    /// False if it requires in-memory evaluation (residual predicate).
    /// </summary>
    public abstract bool IsIndexSatisfiable { get; }
}

/// <summary>
/// A comparison between a document field and a constant value.
/// Examples: c.Age > 25, c.Email == "x@y.com"
/// </summary>
public sealed record ComparisonFilter(
    MemberAccess    Member,
    ExpressionType  Operator,
    object          Value
) : FilterClause
{
    public override bool IsIndexSatisfiable =>
        Member.IndexDefinition is not null && IsSupportedOperator;

    private bool IsSupportedOperator => Operator is
        ExpressionType.Equal            or
        ExpressionType.NotEqual         or
        ExpressionType.GreaterThan      or
        ExpressionType.GreaterThanOrEqual or
        ExpressionType.LessThan         or
        ExpressionType.LessThanOrEqual;
}

/// <summary>
/// AND of two sub-clauses. c.Age > 18 && c.City == "Dallas"
/// </summary>
public sealed record AndFilter(FilterClause Left, FilterClause Right) : FilterClause
{
    public override bool IsIndexSatisfiable => Left.IsIndexSatisfiable || Right.IsIndexSatisfiable;
}

/// <summary>
/// OR of two sub-clauses. c.City == "Dallas" || c.City == "Austin"
/// Index-satisfiable only if BOTH sides use the same index — rare.
/// </summary>
public sealed record OrFilter(FilterClause Left, FilterClause Right) : FilterClause
{
    public override bool IsIndexSatisfiable => false; // conservative — always residual
}

/// <summary>
/// Logical NOT. !c.IsActive
/// </summary>
public sealed record NotFilter(FilterClause Inner) : FilterClause
{
    public override bool IsIndexSatisfiable => false;
}

/// <summary>
/// Method call predicate. c.Name.StartsWith("A"), c.Tags.Contains("vip")
/// </summary>
public sealed record MethodFilter(
    MemberAccess      Target,
    string            MethodName,
    IReadOnlyList<object> Arguments
) : FilterClause
{
    public override bool IsIndexSatisfiable =>
        MethodName is "StartsWith" && Target.IndexDefinition is not null;
    // StartsWith can use a string index range scan
    // Contains, EndsWith, etc. require full scan
}
```

### MemberAccess and OrderClause

```csharp
/// <summary>
/// A resolved field access — the field name and its index definition (if any).
/// </summary>
public sealed record MemberAccess(
    string            FieldName,
    IndexDefinition?  IndexDefinition   // null if field is not indexed
);

public sealed record OrderClause(
    MemberAccess Member,
    bool         Descending
);
```

---

## Layer 2 — TranslatedQuery

```csharp
namespace TreePersistence.Core.Linq.Translation;

/// <summary>
/// The structured output of QueryTranslator.
/// Represents a fully parsed but not-yet-planned query.
/// </summary>
public sealed class TranslatedQuery<TDocument>
{
    /// <summary>All filter clauses from Where calls, in order.</summary>
    public IReadOnlyList<FilterClause>  Filters  { get; init; } = Array.Empty<FilterClause>();

    /// <summary>All sort clauses from OrderBy/ThenBy calls, in order.</summary>
    public IReadOnlyList<OrderClause>   OrderBys { get; init; } = Array.Empty<OrderClause>();

    public int?  Take { get; init; }
    public int?  Skip { get; init; }

    /// <summary>
    /// Predicate covering clauses not satisfiable by any index.
    /// Compiled at plan time and applied in-memory after index results.
    /// Null if all clauses are index-satisfiable.
    /// </summary>
    public Func<TDocument, bool>? ResidualPredicate { get; set; }

    /// <summary>The index scan specification selected by the planner. Null = full scan.</summary>
    public IndexScanSpec? SelectedIndexScan { get; set; }
}

/// <summary>
/// Describes an index scan — what to scan and its bounds.
/// </summary>
public sealed class IndexScanSpec
{
    public required IndexDefinition Index   { get; init; }
    public object?                  From    { get; init; }  // inclusive lower bound, null = unbounded
    public object?                  To      { get; init; }  // inclusive upper bound, null = unbounded
    public bool                     IsPoint { get; init; }  // true = equality lookup
    public bool                     IncludeFrom { get; init; } = true;
    public bool                     IncludeTo   { get; init; } = true;
}
```

---

## Layer 3 — ExpressionEvaluator

Handles constant extraction from expression trees, including closures:

```csharp
namespace TreePersistence.Core.Linq.Translation;

/// <summary>
/// Evaluates an expression to a concrete value.
/// Handles: ConstantExpression, MemberExpression (captured closure variables),
///          and simple arithmetic.
/// </summary>
public static class ExpressionEvaluator
{
    /// <summary>
    /// Evaluates an expression that is expected to resolve to a constant.
    /// Compiles and invokes closures automatically.
    /// Throws NotSupportedInQueryException for complex expressions.
    /// </summary>
    public static object Evaluate(Expression expr)
    {
        switch (expr)
        {
            case ConstantExpression constant:
                return constant.Value!;

            case MemberExpression member when member.Expression is ConstantExpression closure:
                // Captured variable: int x = 25; Where(c => c.Age > x)
                // x is accessed via MemberExpression on the closure object
                return member.Member switch
                {
                    FieldInfo    fi => fi.GetValue(((ConstantExpression)member.Expression).Value),
                    PropertyInfo pi => pi.GetValue(((ConstantExpression)member.Expression).Value),
                    _ => CompileAndInvoke(expr)
                } ?? throw new InvalidOperationException("Null constant in query.");

            case UnaryExpression { NodeType: ExpressionType.Convert } convert:
                // (double)25 — type conversion on a constant
                var inner = Evaluate(convert.Operand);
                return Convert.ChangeType(inner, convert.Type);

            default:
                // Last resort: compile and invoke
                return CompileAndInvoke(expr);
        }
    }

    private static object CompileAndInvoke(Expression expr)
    {
        try
        {
            return Expression.Lambda(expr).Compile().DynamicInvoke()
                   ?? throw new InvalidOperationException("Expression evaluated to null.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new NotSupportedInQueryException(
                $"Cannot evaluate expression: {expr}", ex);
        }
    }
}
```

---

## Layer 4 — QueryTranslator

```csharp
namespace TreePersistence.Core.Linq.Translation;

/// <summary>
/// Walks a LINQ expression tree and produces a TranslatedQuery.
/// One instance per query — not reusable.
/// </summary>
public sealed class QueryTranslator<TDocument>
{
    private readonly IDocumentIndexRegistry<TDocument> _registry;

    private readonly List<FilterClause>  _filters   = new();
    private readonly List<OrderClause>   _orderBys  = new();
    private int? _take;
    private int? _skip;

    public QueryTranslator(IDocumentIndexRegistry<TDocument> registry)
    {
        _registry = registry;
    }

    public TranslatedQuery<TDocument> Translate(Expression expression)
    {
        Visit(expression);
        return new TranslatedQuery<TDocument>
        {
            Filters  = _filters.AsReadOnly(),
            OrderBys = _orderBys.AsReadOnly(),
            Take     = _take,
            Skip     = _skip,
        };
    }

    // ── Visitor Entry ────────────────────────────────────────────────────

    private void Visit(Expression node)
    {
        switch (node)
        {
            case ConstantExpression { Value: IQueryable }:
                return; // base of the chain — nothing to do

            case MethodCallExpression call:
                VisitMethodCall(call);
                return;

            default:
                throw new NotSupportedInQueryException(
                    $"Unsupported expression type at query root: {node.GetType().Name}");
        }
    }

    // ── LINQ Method Dispatch ─────────────────────────────────────────────

    private void VisitMethodCall(MethodCallExpression call)
    {
        // Recurse into the source first (left-to-right chain order)
        Visit(call.Arguments[0]);

        switch (call.Method.Name)
        {
            case nameof(Queryable.Where):
                var lambda  = UnwrapLambda(call.Arguments[1]);
                var clause  = VisitPredicate(lambda.Body, lambda.Parameters[0]);
                _filters.Add(clause);
                break;

            case nameof(Queryable.OrderBy):
                _orderBys.Add(BuildOrderClause(call, descending: false));
                break;

            case nameof(Queryable.OrderByDescending):
                _orderBys.Add(BuildOrderClause(call, descending: true));
                break;

            case nameof(Queryable.ThenBy):
                _orderBys.Add(BuildOrderClause(call, descending: false));
                break;

            case nameof(Queryable.ThenByDescending):
                _orderBys.Add(BuildOrderClause(call, descending: true));
                break;

            case nameof(Queryable.Take):
                _take = (int)ExpressionEvaluator.Evaluate(call.Arguments[1]);
                break;

            case nameof(Queryable.Skip):
                _skip = (int)ExpressionEvaluator.Evaluate(call.Arguments[1]);
                break;

            case nameof(Queryable.Distinct):
                // Passthrough — document IDs are always distinct via primary key
                break;

            // Explicitly unsupported operators — fail fast with clear message
            case nameof(Queryable.Select):
                throw new NotSupportedInQueryException(
                    "Select (projection) is not supported. Queries return full documents. " +
                    "Apply projection after ToListAsync().");

            case nameof(Queryable.Join):
            case nameof(Queryable.GroupJoin):
                throw new NotSupportedInQueryException(
                    "Join operations are not supported. This is a document database.");

            case nameof(Queryable.GroupBy):
                throw new NotSupportedInQueryException(
                    "GroupBy requires a full scan and in-memory aggregation. " +
                    "Use ToListAsync() then LINQ-to-objects GroupBy.");

            default:
                throw new NotSupportedInQueryException(
                    $"LINQ operator '{call.Method.Name}' is not supported.");
        }
    }

    // ── Predicate Visitor ────────────────────────────────────────────────

    private FilterClause VisitPredicate(Expression body, ParameterExpression param)
    {
        switch (body)
        {
            // c.Age > 25, c.Email == "x"
            case BinaryExpression binary when IsComparisonOperator(binary.NodeType):
                return VisitComparison(binary, param);

            // c.A && c.B
            case BinaryExpression { NodeType: ExpressionType.AndAlso } and:
                return new AndFilter(
                    VisitPredicate(and.Left, param),
                    VisitPredicate(and.Right, param));

            // c.A || c.B
            case BinaryExpression { NodeType: ExpressionType.OrElse } or:
                return new OrFilter(
                    VisitPredicate(or.Left, param),
                    VisitPredicate(or.Right, param));

            // !c.Active
            case UnaryExpression { NodeType: ExpressionType.Not } not:
                return new NotFilter(VisitPredicate(not.Operand, param));

            // c.Name.StartsWith("A"), c.Tags.Contains("vip")
            case MethodCallExpression methodCall:
                return VisitMethodPredicate(methodCall, param);

            // c.IsActive (boolean property directly)
            case MemberExpression member when member.Type == typeof(bool):
                return new ComparisonFilter(
                    ResolveMemberAccess(member, param),
                    ExpressionType.Equal,
                    true);

            default:
                throw new NotSupportedInQueryException(
                    $"Unsupported predicate expression: {body}");
        }
    }

    private FilterClause VisitComparison(BinaryExpression binary, ParameterExpression param)
    {
        MemberExpression? memberExpr;
        Expression        valueExpr;
        bool              flipped;

        // Determine which side is the member access and which is the value
        if (IsMemberOnParam(binary.Left, param))
        {
            memberExpr = (MemberExpression)binary.Left;
            valueExpr  = binary.Right;
            flipped    = false;
        }
        else if (IsMemberOnParam(binary.Right, param))
        {
            memberExpr = (MemberExpression)binary.Right;
            valueExpr  = binary.Left;
            flipped    = true;
        }
        else
        {
            // Neither side is a document field — evaluate as constant bool
            var result = (bool)ExpressionEvaluator.Evaluate(binary);
            return result
                ? new ComparisonFilter(new MemberAccess("__const", null), ExpressionType.Equal, true)
                : new ComparisonFilter(new MemberAccess("__const", null), ExpressionType.Equal, false);
        }

        var member   = ResolveMemberAccess(memberExpr, param);
        var value    = ExpressionEvaluator.Evaluate(valueExpr);
        var op       = flipped ? FlipOperator(binary.NodeType) : binary.NodeType;

        return new ComparisonFilter(member, op, value);
    }

    private FilterClause VisitMethodPredicate(
        MethodCallExpression call, ParameterExpression param)
    {
        var targetExpr = call.Object ?? call.Arguments[0];

        if (!IsMemberOnParam(targetExpr, param))
            throw new NotSupportedInQueryException(
                $"Method '{call.Method.Name}' is only supported when called on a document field.");

        var member = ResolveMemberAccess((MemberExpression)targetExpr, param);
        var args   = call.Arguments
            .Skip(call.Object is null ? 1 : 0)
            .Select(ExpressionEvaluator.Evaluate)
            .ToList();

        return call.Method.Name switch
        {
            "StartsWith" => new MethodFilter(member, "StartsWith", args),
            "EndsWith"   => new MethodFilter(member, "EndsWith",   args),
            "Contains"   => new MethodFilter(member, "Contains",   args),
            "Equals"     => new ComparisonFilter(member, ExpressionType.Equal, args[0]),
            _ => throw new NotSupportedInQueryException(
                $"String method '{call.Method.Name}' is not supported in queries.")
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private MemberAccess ResolveMemberAccess(
        MemberExpression memberExpr, ParameterExpression param)
    {
        // Only direct property access on the parameter is supported
        // Nested: c.Address.City — throw
        if (memberExpr.Expression != param)
            throw new NotSupportedInQueryException(
                $"Nested property access '{memberExpr}' is not supported. " +
                $"Only direct field access (e.g. c.Age) is supported.");

        var fieldName = memberExpr.Member.Name;
        var indexDef  = _registry.FindByField(fieldName); // null if not indexed
        return new MemberAccess(fieldName, indexDef);
    }

    private OrderClause BuildOrderClause(MethodCallExpression call, bool descending)
    {
        var lambda = UnwrapLambda(call.Arguments[1]);
        if (lambda.Body is not MemberExpression member)
            throw new NotSupportedInQueryException(
                "OrderBy requires a direct field access expression.");

        return new OrderClause(
            ResolveMemberAccess(member, lambda.Parameters[0]),
            descending);
    }

    private static bool IsMemberOnParam(Expression expr, ParameterExpression param) =>
        expr is MemberExpression m && m.Expression == param;

    private static LambdaExpression UnwrapLambda(Expression expr) =>
        (LambdaExpression)((UnaryExpression)expr).Operand;

    private static bool IsComparisonOperator(ExpressionType t) => t is
        ExpressionType.Equal            or ExpressionType.NotEqual       or
        ExpressionType.GreaterThan      or ExpressionType.GreaterThanOrEqual or
        ExpressionType.LessThan         or ExpressionType.LessThanOrEqual;

    private static ExpressionType FlipOperator(ExpressionType op) => op switch
    {
        ExpressionType.GreaterThan          => ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual   => ExpressionType.LessThanOrEqual,
        ExpressionType.LessThan             => ExpressionType.GreaterThan,
        ExpressionType.LessThanOrEqual      => ExpressionType.GreaterThanOrEqual,
        _                                   => op
    };
}

public sealed class NotSupportedInQueryException(string message, Exception? inner = null)
    : Exception(message, inner);
```

### Tests: QueryTranslator

```
GIVEN c => c.Age > 25
WHEN Translate is called
THEN Filters contains one ComparisonFilter(Age, GreaterThan, 25)
THEN Filter.Member.IndexDefinition is not null if Age is indexed

GIVEN c => c.Age > 25 && c.City == "Dallas"
WHEN Translate is called
THEN Filters contains AndFilter
THEN Left = ComparisonFilter(Age, GreaterThan, 25)
THEN Right = ComparisonFilter(City, Equal, "Dallas")

GIVEN c => c.Age > x where x is a captured int variable
WHEN Translate is called
THEN ComparisonFilter.Value == the value of x (closure evaluated correctly)

GIVEN 25 < c.Age (reversed)
WHEN Translate is called
THEN ComparisonFilter(Age, GreaterThan, 25) — operator flipped correctly

GIVEN .OrderBy(c => c.LastName).ThenByDescending(c => c.Age)
WHEN Translate is called
THEN OrderBys[0] = OrderClause(LastName, Descending=false)
THEN OrderBys[1] = OrderClause(Age, Descending=true)

GIVEN .Take(10).Skip(5)
WHEN Translate is called
THEN Take == 10, Skip == 5

GIVEN .Select(c => c.Name)
WHEN Translate is called
THEN throws NotSupportedInQueryException with message mentioning "projection"

GIVEN c.Name.StartsWith("A")
WHEN Translate is called
THEN Filters contains MethodFilter(Name, "StartsWith", ["A"])

GIVEN c.Address.City == "Dallas" (nested access)
WHEN Translate is called
THEN throws NotSupportedInQueryException with message mentioning "nested property"

GIVEN c => !c.IsActive
WHEN Translate is called
THEN Filters contains NotFilter wrapping ComparisonFilter(IsActive, Equal, true)
```

---

## Layer 5 — Query Planner

```csharp
namespace TreePersistence.Core.Linq.Planning;

/// <summary>
/// Selects the optimal execution plan for a translated query.
/// Considers index availability, operator types, and sort requirements.
/// </summary>
public sealed class QueryPlanner<TDocument>
{
    private readonly IDocumentIndexRegistry<TDocument> _registry;
    private readonly IQueryDiagnostics?                _diagnostics;

    public QueryPlanner(
        IDocumentIndexRegistry<TDocument> registry,
        IQueryDiagnostics? diagnostics = null)
    {
        _registry    = registry;
        _diagnostics = diagnostics;
    }

    public ExecutionPlan<TDocument> Plan(TranslatedQuery<TDocument> query)
    {
        var indexSpec = SelectBestIndex(query.Filters);

        if (indexSpec is null)
        {
            // No usable index — full scan with in-memory predicate
            var predicate = BuildResidualPredicate(query.Filters);

            _diagnostics?.ReportFullScan(typeof(TDocument).Name, query);

            return new FullScanPlan<TDocument>(predicate, query.Take, query.Skip);
        }

        // Some filters are index-satisfiable, rest become residual
        var (primaryScan, residualFilters) = SplitFilters(query.Filters, indexSpec);
        var residual = BuildResidualPredicate(residualFilters);
        bool needsSort = RequiresSortStep(indexSpec, query.OrderBys);

        _diagnostics?.ReportIndexScan(typeof(TDocument).Name, indexSpec, residual is not null);

        return indexSpec.IsPoint
            ? new IndexPointLookupPlan<TDocument>(
                _registry.GetExecutor(indexSpec.Index),
                indexSpec,
                residual,
                query.Take,
                query.Skip)
            : new IndexRangeScanPlan<TDocument>(
                _registry.GetExecutor(indexSpec.Index),
                indexSpec,
                residual,
                query.Take,
                query.Skip,
                needsSort ? query.OrderBys : Array.Empty<OrderClause>());
    }

    // ── Index Selection ──────────────────────────────────────────────────

    private static IndexScanSpec? SelectBestIndex(IReadOnlyList<FilterClause> filters)
    {
        IndexScanSpec? best = null;

        foreach (var filter in FlattenAndClauses(filters))
        {
            if (filter is not ComparisonFilter cmp || !cmp.IsIndexSatisfiable)
                continue;

            var candidate = BuildScanSpec(cmp);
            if (candidate is null) continue;

            // Prefer: unique equality > non-unique equality > range
            if (best is null)                                    { best = candidate; continue; }
            if (!best.IsPoint && candidate.IsPoint)              { best = candidate; continue; }
            if (best.IsPoint && candidate.IsPoint &&
                candidate.Index.IsUnique && !best.Index.IsUnique) { best = candidate; continue; }
        }

        return best;
    }

    private static IndexScanSpec? BuildScanSpec(ComparisonFilter cmp) =>
        cmp.Operator switch
        {
            ExpressionType.Equal =>
                new IndexScanSpec
                {
                    Index   = cmp.Member.IndexDefinition!,
                    From    = cmp.Value,
                    To      = cmp.Value,
                    IsPoint = true,
                },

            ExpressionType.GreaterThan =>
                new IndexScanSpec
                {
                    Index       = cmp.Member.IndexDefinition!,
                    From        = cmp.Value,
                    IsPoint     = false,
                    IncludeFrom = false,
                },

            ExpressionType.GreaterThanOrEqual =>
                new IndexScanSpec
                {
                    Index   = cmp.Member.IndexDefinition!,
                    From    = cmp.Value,
                    IsPoint = false,
                },

            ExpressionType.LessThan =>
                new IndexScanSpec
                {
                    Index     = cmp.Member.IndexDefinition!,
                    To        = cmp.Value,
                    IsPoint   = false,
                    IncludeTo = false,
                },

            ExpressionType.LessThanOrEqual =>
                new IndexScanSpec
                {
                    Index   = cmp.Member.IndexDefinition!,
                    To      = cmp.Value,
                    IsPoint = false,
                },

            _ => null
        };

    // ── Filter Splitting ─────────────────────────────────────────────────

    private static (IndexScanSpec primary, IReadOnlyList<FilterClause> residual)
        SplitFilters(IReadOnlyList<FilterClause> filters, IndexScanSpec indexSpec)
    {
        var residual = new List<FilterClause>();

        foreach (var filter in filters)
        {
            if (filter is ComparisonFilter cmp &&
                cmp.Member.FieldName == indexSpec.Index.FieldName &&
                cmp.IsIndexSatisfiable)
                continue; // covered by index scan

            residual.Add(filter);
        }

        return (indexSpec, residual);
    }

    // ── Residual Predicate Compilation ───────────────────────────────────

    private static Func<TDocument, bool>? BuildResidualPredicate(
        IReadOnlyList<FilterClause> filters)
    {
        if (!filters.Any()) return null;

        var param = Expression.Parameter(typeof(TDocument), "doc");
        var body  = filters
            .Select(f => ClauseToExpression(f, param))
            .Aggregate(Expression.AndAlso);

        return Expression.Lambda<Func<TDocument, bool>>(body, param).Compile();
    }

    private static Expression ClauseToExpression(FilterClause clause, ParameterExpression param)
    {
        return clause switch
        {
            ComparisonFilter cmp =>
                BuildComparisonExpression(cmp, param),

            AndFilter and =>
                Expression.AndAlso(
                    ClauseToExpression(and.Left, param),
                    ClauseToExpression(and.Right, param)),

            OrFilter or =>
                Expression.OrElse(
                    ClauseToExpression(or.Left, param),
                    ClauseToExpression(or.Right, param)),

            NotFilter not =>
                Expression.Not(ClauseToExpression(not.Inner, param)),

            MethodFilter method =>
                BuildMethodExpression(method, param),

            _ => throw new NotSupportedInQueryException(
                $"Cannot build residual expression for {clause.GetType().Name}")
        };
    }

    private static Expression BuildComparisonExpression(
        ComparisonFilter cmp, ParameterExpression param)
    {
        var memberExpr = Expression.PropertyOrField(param, cmp.Member.FieldName);
        var valueExpr  = Expression.Constant(
            Convert.ChangeType(cmp.Value, memberExpr.Type));

        return cmp.Operator switch
        {
            ExpressionType.Equal              => Expression.Equal(memberExpr, valueExpr),
            ExpressionType.NotEqual           => Expression.NotEqual(memberExpr, valueExpr),
            ExpressionType.GreaterThan        => Expression.GreaterThan(memberExpr, valueExpr),
            ExpressionType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(memberExpr, valueExpr),
            ExpressionType.LessThan           => Expression.LessThan(memberExpr, valueExpr),
            ExpressionType.LessThanOrEqual    => Expression.LessThanOrEqual(memberExpr, valueExpr),
            _ => throw new NotSupportedInQueryException(
                $"Operator {cmp.Operator} cannot be compiled to residual expression.")
        };
    }

    private static Expression BuildMethodExpression(
        MethodFilter method, ParameterExpression param)
    {
        var target    = Expression.PropertyOrField(param, method.Target.FieldName);
        var stringArg = Expression.Constant(method.Arguments[0].ToString());
        var methodInfo = typeof(string).GetMethod(method.MethodName, new[] { typeof(string) })!;
        return Expression.Call(target, methodInfo, stringArg);
    }

    private static bool RequiresSortStep(
        IndexScanSpec indexSpec, IReadOnlyList<OrderClause> orderBys)
    {
        if (!orderBys.Any()) return false;
        var first = orderBys[0];
        // Index already provides order if we're scanning the indexed field in the right direction
        return !(first.Member.FieldName == indexSpec.Index.FieldName &&
                 first.Descending == indexSpec.Index.IsDescending);
    }

    private static IEnumerable<FilterClause> FlattenAndClauses(
        IEnumerable<FilterClause> filters)
    {
        foreach (var f in filters)
        {
            if (f is AndFilter and)
            {
                foreach (var inner in FlattenAndClauses(new[] { and.Left, and.Right }))
                    yield return inner;
            }
            else yield return f;
        }
    }
}
```

---

## Layer 6 — Execution Plans

```csharp
namespace TreePersistence.Core.Linq.Planning;

public abstract class ExecutionPlan<TDocument>
{
    public abstract IAsyncEnumerable<TDocument> ExecuteAsync(
        IDocumentCollection<TDocument> collection,
        CancellationToken ct);
}

/// <summary>
/// Full heap scan with in-memory predicate.
/// O(n) — emits a diagnostic warning.
/// </summary>
public sealed class FullScanPlan<TDocument>(
    Func<TDocument, bool>? predicate,
    int? take,
    int? skip) : ExecutionPlan<TDocument>
{
    public override async IAsyncEnumerable<TDocument> ExecuteAsync(
        IDocumentCollection<TDocument> collection,
        [EnumeratorCancellation] CancellationToken ct)
    {
        int skipped = 0;
        int taken   = 0;

        await foreach (var doc in collection.ScanAllAsync(ct))
        {
            if (predicate is not null && !predicate(doc)) continue;

            if (skip.HasValue && skipped < skip.Value) { skipped++; continue; }

            yield return doc;
            taken++;

            if (take.HasValue && taken >= take.Value) yield break;
        }
    }
}

/// <summary>
/// Equality lookup on an index. O(log n).
/// </summary>
public sealed class IndexPointLookupPlan<TDocument>(
    IIndexExecutor<TDocument> executor,
    IndexScanSpec             spec,
    Func<TDocument, bool>?    residual,
    int? take,
    int? skip) : ExecutionPlan<TDocument>
{
    public override async IAsyncEnumerable<TDocument> ExecuteAsync(
        IDocumentCollection<TDocument> collection,
        [EnumeratorCancellation] CancellationToken ct)
    {
        int skipped = 0;
        int taken   = 0;

        await foreach (var id in executor.LookupAsync(spec.From!, ct))
        {
            var doc = await collection.FindAsync(id, ct);
            if (doc is null) continue;
            if (residual is not null && !residual(doc)) continue;

            if (skip.HasValue && skipped < skip.Value) { skipped++; continue; }

            yield return doc;
            taken++;

            if (take.HasValue && taken >= take.Value) yield break;
        }
    }
}

/// <summary>
/// Range scan on an index. O(log n + k) where k = result count.
/// Optionally applies in-memory sort if index order doesn't satisfy OrderBy.
/// </summary>
public sealed class IndexRangeScanPlan<TDocument>(
    IIndexExecutor<TDocument>      executor,
    IndexScanSpec                  spec,
    Func<TDocument, bool>?         residual,
    int?                           take,
    int?                           skip,
    IReadOnlyList<OrderClause>     sortClauses) : ExecutionPlan<TDocument>
{
    public override async IAsyncEnumerable<TDocument> ExecuteAsync(
        IDocumentCollection<TDocument> collection,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var results = new List<TDocument>();

        await foreach (var id in executor.ScanRangeAsync(spec.From, spec.To, ct))
        {
            var doc = await collection.FindAsync(id, ct);
            if (doc is null) continue;
            if (residual is not null && !residual(doc)) continue;
            results.Add(doc);
        }

        // Apply in-memory sort if required
        IEnumerable<TDocument> sorted = results;
        if (sortClauses.Any())
            sorted = ApplySort(results, sortClauses);

        // Apply skip and take
        if (skip.HasValue)  sorted = sorted.Skip(skip.Value);
        if (take.HasValue)  sorted = sorted.Take(take.Value);

        foreach (var doc in sorted)
            yield return doc;
    }

    private static IOrderedEnumerable<TDocument> ApplySort(
        List<TDocument> docs, IReadOnlyList<OrderClause> clauses)
    {
        IOrderedEnumerable<TDocument>? ordered = null;

        foreach (var clause in clauses)
        {
            // Build a compiled property accessor for the sort field
            var param    = Expression.Parameter(typeof(TDocument));
            var member   = Expression.PropertyOrField(param, clause.Member.FieldName);
            var keyType  = member.Type;
            var lambda   = Expression.Lambda(member, param);

            // Use reflection to call Enumerable.OrderBy/ThenBy with the right types
            ordered = ordered is null
                ? OrderFirst(docs, lambda, keyType, clause.Descending)
                : OrderThen(ordered, lambda, keyType, clause.Descending);
        }

        return ordered!;
    }

    private static IOrderedEnumerable<TDocument> OrderFirst(
        List<TDocument> docs, LambdaExpression keySelector, Type keyType, bool desc)
    {
        var compiled = keySelector.Compile();
        Func<TDocument, object?> extractor = d => compiled.DynamicInvoke(d);

        return desc
            ? docs.OrderByDescending(extractor)
            : docs.OrderBy(extractor);
    }

    private static IOrderedEnumerable<TDocument> OrderThen(
        IOrderedEnumerable<TDocument> source, LambdaExpression keySelector,
        Type keyType, bool desc)
    {
        var compiled = keySelector.Compile();
        Func<TDocument, object?> extractor = d => compiled.DynamicInvoke(d);

        return desc
            ? source.ThenByDescending(extractor)
            : source.ThenBy(extractor);
    }
}
```

---

## Layer 7 — IQueryDiagnostics

```csharp
namespace TreePersistence.Core.Linq.Planning;

/// <summary>
/// Optional diagnostics sink. Register in DI to receive query plan information.
/// Useful for identifying missing indexes causing full scans.
/// </summary>
public interface IQueryDiagnostics
{
    void ReportIndexScan(string collectionName, IndexScanSpec spec, bool hasResidual);

    /// <summary>
    /// Called when a full scan is planned — this is the "missing index" warning.
    /// Implementations should log at Warning level.
    /// </summary>
    void ReportFullScan(string collectionName, TranslatedQuery<object> query);
}

/// <summary>Default implementation — logs via ILogger.</summary>
public sealed class LoggingQueryDiagnostics(ILogger<LoggingQueryDiagnostics> logger)
    : IQueryDiagnostics
{
    public void ReportIndexScan(string col, IndexScanSpec spec, bool hasResidual) =>
        logger.LogDebug(
            "[{Collection}] Index scan on '{Index}' " +
            "(point={IsPoint}, residual={HasResidual})",
            col, spec.Index.Name, spec.IsPoint, hasResidual);

    public void ReportFullScan(string col, TranslatedQuery<object> query) =>
        logger.LogWarning(
            "[{Collection}] Full collection scan — consider adding an index. " +
            "Filters: {FilterCount}",
            col, query.Filters.Count);
}
```

---

## Layer 8 — DocumentQueryable and DocumentQueryProvider

### DocumentQueryable

```csharp
namespace TreePersistence.Core.Linq;

public sealed class DocumentQueryable<TDocument> : IQueryable<TDocument>
    where TDocument : class
{
    internal DocumentQueryProvider<TDocument> TypedProvider { get; }

    public DocumentQueryable(DocumentQueryProvider<TDocument> provider)
    {
        TypedProvider = provider;
        Expression    = Expression.Constant(this);
    }

    internal DocumentQueryable(
        DocumentQueryProvider<TDocument> provider,
        Expression expression)
    {
        TypedProvider = provider;
        Expression    = expression;
    }

    public Type           ElementType => typeof(TDocument);
    public Expression     Expression  { get; }
    public IQueryProvider Provider    => TypedProvider;

    // Sync enumerator — forces evaluation, prefer async
    public IEnumerator<TDocument> GetEnumerator() =>
        TypedProvider
            .Execute<IEnumerable<TDocument>>(Expression)
            .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

### DocumentQueryProvider

```csharp
public sealed class DocumentQueryProvider<TDocument> : IQueryProvider
    where TDocument : class
{
    private readonly IDocumentCollection<TDocument>    _collection;
    private readonly QueryTranslator<TDocument>        _translator;
    private readonly QueryPlanner<TDocument>           _planner;

    public DocumentQueryProvider(
        IDocumentCollection<TDocument>    collection,
        IDocumentIndexRegistry<TDocument> registry,
        IQueryDiagnostics?                diagnostics = null)
    {
        _collection  = collection;
        _translator  = new QueryTranslator<TDocument>(registry);
        _planner     = new QueryPlanner<TDocument>(registry, diagnostics);
    }

    // IQueryProvider — called by LINQ operators like Where, OrderBy
    public IQueryable CreateQuery(Expression expression) =>
        new DocumentQueryable<TDocument>(this, expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        if (typeof(TElement) != typeof(TDocument))
            throw new NotSupportedInQueryException(
                "Type change via Select is not supported.");
        return (IQueryable<TElement>)(object)
            new DocumentQueryable<TDocument>(this, expression);
    }

    // IQueryProvider — called by terminal operators (sync)
    public object? Execute(Expression expression) =>
        Execute<IEnumerable<TDocument>>(expression);

    public TResult Execute<TResult>(Expression expression)
    {
        var query = _translator.Translate(expression);
        var plan  = _planner.Plan(query);
        var docs  = plan.ExecuteAsync(_collection, CancellationToken.None)
            .ToBlockingEnumerable()
            .ToList();
        return (TResult)(object)docs;
    }

    // Async path — used by extension methods
    public IAsyncEnumerable<TDocument> ExecuteAsync(
        Expression expression,
        CancellationToken ct = default)
    {
        var query = _translator.Translate(expression);
        var plan  = _planner.Plan(query);
        return plan.ExecuteAsync(_collection, ct);
    }
}
```

---

## Layer 9 — Async Extension Methods

```csharp
namespace TreePersistence.Core.Linq;

public static class DocumentQueryableExtensions
{
    // ── Terminal operators (async) ────────────────────────────────────────

    public static async ValueTask<List<TDocument>> ToListAsync<TDocument>(
        this IQueryable<TDocument> source,
        CancellationToken ct = default)
        where TDocument : class
    {
        var results = new List<TDocument>();
        await foreach (var doc in source.ToAsyncEnumerable(ct))
            results.Add(doc);
        return results;
    }

    public static async ValueTask<TDocument?> FirstOrDefaultAsync<TDocument>(
        this IQueryable<TDocument> source,
        CancellationToken ct = default)
        where TDocument : class
    {
        await foreach (var doc in source.ToAsyncEnumerable(ct))
            return doc;
        return default;
    }

    public static async ValueTask<TDocument> FirstAsync<TDocument>(
        this IQueryable<TDocument> source,
        CancellationToken ct = default)
        where TDocument : class
    {
        await foreach (var doc in source.ToAsyncEnumerable(ct))
            return doc;
        throw new InvalidOperationException("Sequence contains no elements.");
    }

    public static async ValueTask<TDocument?> SingleOrDefaultAsync<TDocument>(
        this IQueryable<TDocument> source,
        CancellationToken ct = default)
        where TDocument : class
    {
        TDocument? result = default;
        bool found = false;

        await foreach (var doc in source.ToAsyncEnumerable(ct))
        {
            if (found)
                throw new InvalidOperationException(
                    "Sequence contains more than one element.");
            result = doc;
            found  = true;
        }

        return result;
    }

    public static async ValueTask<int> CountAsync<TDocument>(
        this IQueryable<TDocument> source,
        CancellationToken ct = default)
        where TDocument : class
    {
        int count = 0;
        await foreach (var _ in source.ToAsyncEnumerable(ct))
            count++;
        return count;
    }

    public static async ValueTask<bool> AnyAsync<TDocument>(
        this IQueryable<TDocument> source,
        CancellationToken ct = default)
        where TDocument : class
    {
        await foreach (var _ in source.ToAsyncEnumerable(ct))
            return true;
        return false;
    }

    public static async ValueTask<bool> AnyAsync<TDocument>(
        this IQueryable<TDocument> source,
        Expression<Func<TDocument, bool>> predicate,
        CancellationToken ct = default)
        where TDocument : class =>
        await source.Where(predicate).AnyAsync(ct);

    public static async ValueTask<bool> AllAsync<TDocument>(
        this IQueryable<TDocument> source,
        Expression<Func<TDocument, bool>> predicate,
        CancellationToken ct = default)
        where TDocument : class
    {
        var compiled = predicate.Compile();
        await foreach (var doc in source.ToAsyncEnumerable(ct))
            if (!compiled(doc)) return false;
        return true;
    }

    /// <summary>
    /// Streaming enumerable — does not buffer. Best for large result sets.
    /// </summary>
    public static IAsyncEnumerable<TDocument> ToAsyncEnumerable<TDocument>(
        this IQueryable<TDocument> source,
        CancellationToken ct = default)
        where TDocument : class
    {
        if (source is DocumentQueryable<TDocument> dq)
            return dq.TypedProvider.ExecuteAsync(source.Expression, ct);

        throw new InvalidOperationException(
            $"ToAsyncEnumerable requires a {nameof(DocumentQueryable<TDocument>)}.");
    }

    // ── Convenience: AsQueryable extension on IDocumentCollection ────────

    public static IQueryable<TDocument> AsQueryable<TDocument>(
        this IDocumentCollection<TDocument> collection,
        IDocumentIndexRegistry<TDocument> registry,
        IQueryDiagnostics? diagnostics = null)
        where TDocument : class
    {
        var provider = new DocumentQueryProvider<TDocument>(collection, registry, diagnostics);
        return new DocumentQueryable<TDocument>(provider);
    }
}
```

---

## Tests: Query Planner

```
GIVEN query with Where(c => c.Age == 25) and Age is indexed
WHEN Plan is called
THEN returns IndexPointLookupPlan
THEN IndexScanSpec.IsPoint == true
THEN IndexScanSpec.From == 25

GIVEN query with Where(c => c.Age > 18 && c.Age < 65) and Age is indexed
WHEN Plan is called
THEN returns IndexRangeScanPlan
THEN IndexScanSpec.From == 18 (exclusive)
THEN IndexScanSpec.To == 65 (exclusive)

GIVEN query with Where(c => c.Age > 18 && c.City == "Dallas")
     Age is indexed, City is NOT indexed
WHEN Plan is called
THEN returns IndexRangeScanPlan using Age index
THEN ResidualPredicate is not null
THEN ResidualPredicate(customer{City="Dallas"}) == true
THEN ResidualPredicate(customer{City="Austin"}) == false

GIVEN query with Where(c => c.City == "Dallas") and City is NOT indexed
WHEN Plan is called
THEN returns FullScanPlan
THEN IQueryDiagnostics.ReportFullScan is called

GIVEN equality on a unique index vs equality on a non-unique index
WHEN Plan selects best index
THEN unique index is preferred

GIVEN query with OrderBy(c => c.Age) and Age index exists (ascending)
WHEN Plan is called
THEN IndexRangeScanPlan.SortClauses is empty (index already ordered)

GIVEN query with OrderByDescending(c => c.Age) and Age index is ascending
WHEN Plan is called
THEN IndexRangeScanPlan.SortClauses contains the descending clause
```

---

## Tests: DocumentQueryable Integration

```
GIVEN a collection with 100 customers, Age indexed
WHEN .Where(c => c.Age == 25).ToListAsync()
THEN only customers with Age == 25 are returned
THEN index scan was used (not full scan) — verified via diagnostics mock

GIVEN .Where(c => c.Age >= 18).Take(10).ToListAsync()
THEN exactly 10 documents returned
THEN all have Age >= 18

GIVEN .Where(c => c.Age > 18 && c.City == "Dallas").ToListAsync()
THEN all returned customers have Age > 18 AND City == "Dallas"

GIVEN .OrderBy(c => c.LastName).ToListAsync()
THEN results are sorted by LastName ascending

GIVEN .Where(c => c.Age > 18).OrderBy(c => c.Age).Skip(5).Take(10).ToListAsync()
THEN 10 results returned, starting from the 6th oldest adult

GIVEN FirstOrDefaultAsync on empty result set
THEN returns null

GIVEN SingleOrDefaultAsync on two matching documents
THEN throws InvalidOperationException

GIVEN ToAsyncEnumerable on large result set
WHEN iterated partially (break after 3)
THEN underlying scan stops after 3 documents (lazy evaluation)

GIVEN AnyAsync(c => c.Age > 100)
WHEN no such document exists
THEN returns false without scanning all documents

GIVEN a collection with IQueryDiagnostics registered
WHEN a query with no applicable index is executed
THEN ReportFullScan is called once with correct collection name
```

---

## DI Registration

```csharp
// Updated AddDocumentCollection to wire up LINQ provider
public static IServiceCollection AddDocumentCollection<TDocument>(
    this IServiceCollection services,
    Action<DocumentCollectionBuilder<TDocument>> configure)
    where TDocument : class
{
    // ... existing registration ...

    // LINQ provider — created on demand from the collection + registry
    services.AddTransient<DocumentQueryProvider<TDocument>>(sp =>
        new DocumentQueryProvider<TDocument>(
            sp.GetRequiredService<IDocumentCollection<TDocument>>(),
            sp.GetRequiredService<IDocumentIndexRegistry<TDocument>>(),
            sp.GetService<IQueryDiagnostics>())); // optional

    return services;
}

// Optional diagnostics
public static IServiceCollection AddQueryDiagnostics(
    this IServiceCollection services)
{
    services.AddSingleton<IQueryDiagnostics, LoggingQueryDiagnostics>();
    return services;
}
```

### Full Usage Example

```csharp
// Registration
builder.Services
    .AddMmapBPlusTree<Guid, HeapAddress>("customers.tpd", 2_147_483_648)
    .AddWal("customers.tpw", IsolationLevel.SnapshotMVCC)
    .AddCheckpointService()
    .AddAutoVacuum()
    .AddDocumentCollection<Customer>(col =>
    {
        col.HasPrimaryKey(c => c.Id);
        col.HasIndex(c => c.Age);
        col.HasIndex(c => c.Email, o => o.IsUnique = true);
        col.HasIndex(c => c.LastName);
    })
    .AddQueryDiagnostics();

// Query usage via DI
public class CustomerService(
    IDocumentCollection<Customer> customers,
    IDocumentIndexRegistry<Customer> registry)
{
    private IQueryable<Customer> Query() => customers.AsQueryable(registry);

    // Index point lookup — O(log n)
    public Task<Customer?> FindByEmailAsync(string email, CancellationToken ct) =>
        Query()
            .Where(c => c.Email == email)
            .FirstOrDefaultAsync(ct);

    // Index range scan — O(log n + k)
    public Task<List<Customer>> FindAdultsAsync(CancellationToken ct) =>
        Query()
            .Where(c => c.Age >= 18)
            .OrderBy(c => c.LastName)
            .ToListAsync(ct);

    // Mixed — Age index used, City is residual
    public Task<List<Customer>> FindLocalAdultsAsync(string city, CancellationToken ct) =>
        Query()
            .Where(c => c.Age >= 18 && c.City == city)
            .Take(100)
            .ToListAsync(ct);

    // Streaming — no buffering
    public IAsyncEnumerable<Customer> StreamAllAsync(CancellationToken ct) =>
        Query().ToAsyncEnumerable(ct);
}
```

---

## What Is Not Supported

Document these clearly so callers get helpful errors rather than silent wrong results:

| Operator | Status | Reason |
|---|---|---|
| `Select` (projection) | ❌ Throws | Would require schema knowledge at storage level |
| `Join` / `GroupJoin` | ❌ Throws | Document database — no joins |
| `GroupBy` | ❌ Throws | Use ToListAsync then LINQ-to-objects |
| `Sum` / `Max` / `Min` / `Average` | ❌ Throws | Aggregates require full scan — use ToListAsync |
| Nested property access | ❌ Throws | `c.Address.City` — only direct field access |
| `Contains` on collection field | ❌ Throws | Requires inverted index (future spec) |
| `EndsWith` | ⚠️ Full scan | String index only supports prefix (StartsWith) |
| `OrderBy` on non-indexed field | ⚠️ In-memory sort | Works but buffered — logged as warning |
| `OR` across different fields | ⚠️ Full scan | Index can't satisfy OR across different fields |
| `Count()` (sync) | ⚠️ Full scan | Use `CountAsync()` — sync forces complete enumeration |

---

## Acceptance Criteria

```
Translation
✅ All comparison operators translate correctly including flipped form (25 < c.Age)
✅ && translates to AndFilter, || to OrFilter, ! to NotFilter
✅ Captured closure variables evaluated correctly
✅ StartsWith translates to MethodFilter
✅ Unsupported operators throw NotSupportedInQueryException with helpful message
✅ Nested property access throws with clear error
✅ Boolean member access (c.IsActive) treated as equality to true

Planning
✅ Indexed equality → IndexPointLookupPlan
✅ Indexed range → IndexRangeScanPlan
✅ No applicable index → FullScanPlan + diagnostics reported
✅ Unique index preferred over non-unique for equality
✅ Residual predicate compiled correctly for non-indexed conditions
✅ OrderBy on index field does not add sort step
✅ OrderBy on non-index field adds in-memory sort step

Execution
✅ IndexPointLookupPlan returns only matching documents
✅ IndexRangeScanPlan returns only documents in range
✅ FullScanPlan applies predicate correctly
✅ Take/Skip applied correctly in all plan types
✅ In-memory sort produces correct ordering
✅ Lazy evaluation — streaming stops when Take is satisfied

Async Extensions
✅ ToListAsync returns all results as list
✅ FirstOrDefaultAsync returns first or null
✅ FirstAsync throws on empty
✅ SingleOrDefaultAsync throws on multiple results
✅ CountAsync returns correct count
✅ AnyAsync returns false without full scan when no match
✅ ToAsyncEnumerable is lazy — no buffering

Integration
✅ Full pipeline: insert documents → query via LINQ → correct results
✅ Index scan used for indexed fields — confirmed via IQueryDiagnostics mock
✅ Full scan logged as warning when no index available
✅ MVCC isolation: queries see consistent snapshot
✅ Concurrent inserts and queries produce consistent results
```

---

## Implementation Order

1. `FilterClause` hierarchy + `MemberAccess` + `OrderClause` records + unit tests
2. `TranslatedQuery<T>` + `IndexScanSpec` records
3. `ExpressionEvaluator` + closure evaluation tests
4. `QueryTranslator<T>` — Where/comparison handling + tests
5. `QueryTranslator<T>` — OrderBy/Take/Skip handling + tests
6. `QueryTranslator<T>` — method predicates (StartsWith etc.) + tests
7. `QueryTranslator<T>` — unsupported operator error paths + tests
8. `IQueryDiagnostics` + `LoggingQueryDiagnostics`
9. `QueryPlanner<T>` — index selection logic + tests
10. `QueryPlanner<T>` — filter splitting + residual predicate compilation + tests
11. `QueryPlanner<T>` — sort step determination + tests
12. `FullScanPlan<T>` + `IndexPointLookupPlan<T>` + tests
13. `IndexRangeScanPlan<T>` including in-memory sort + tests
14. `DocumentQueryable<T>` + `DocumentQueryProvider<T>`
15. All async extension methods + tests
16. DI wiring + `AddQueryDiagnostics`
17. Integration tests — full query pipeline with real collection and indexes
18. Integration tests — MVCC + concurrent query correctness
```
