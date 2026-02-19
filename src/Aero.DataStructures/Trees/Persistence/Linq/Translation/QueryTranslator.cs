using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Aero.DataStructures.Trees.Persistence.Indexes;

namespace Aero.DataStructures.Trees.Persistence.Linq.Translation;

public sealed class QueryTranslator<TDocument>
    where TDocument : class
{
    private readonly IDocumentIndexRegistry<TDocument> _registry;

    private readonly List<FilterClause> _filters = new();
    private readonly List<OrderClause> _orderBys = new();
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
            Filters = _filters.AsReadOnly(),
            OrderBys = _orderBys.AsReadOnly(),
            Take = _take,
            Skip = _skip,
        };
    }

    private void Visit(Expression node)
    {
        switch (node)
        {
            case ConstantExpression { Value: IQueryable }:
                return;

            case MethodCallExpression call:
                VisitMethodCall(call);
                return;

            default:
                throw new NotSupportedInQueryException(
                    $"Unsupported expression type at query root: {node.GetType().Name}");
        }
    }

    private void VisitMethodCall(MethodCallExpression call)
    {
        Visit(call.Arguments[0]);

        switch (call.Method.Name)
        {
            case nameof(Queryable.Where):
                var lambda = UnwrapLambda(call.Arguments[1]);
                var clause = VisitPredicate(lambda.Body, lambda.Parameters[0]);
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
                break;

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

    private FilterClause VisitPredicate(Expression body, ParameterExpression param)
    {
        switch (body)
        {
            case BinaryExpression binary when IsComparisonOperator(binary.NodeType):
                return VisitComparison(binary, param);

            case BinaryExpression { NodeType: ExpressionType.AndAlso } and:
                return new AndFilter(
                    VisitPredicate(and.Left, param),
                    VisitPredicate(and.Right, param));

            case BinaryExpression { NodeType: ExpressionType.OrElse } or:
                return new OrFilter(
                    VisitPredicate(or.Left, param),
                    VisitPredicate(or.Right, param));

            case UnaryExpression { NodeType: ExpressionType.Not } not:
                return new NotFilter(VisitPredicate(not.Operand, param));

            case MethodCallExpression methodCall:
                return VisitMethodPredicate(methodCall, param);

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
        Expression valueExpr;
        bool flipped;

        if (IsMemberOnParam(binary.Left, param))
        {
            memberExpr = (MemberExpression)binary.Left;
            valueExpr = binary.Right;
            flipped = false;
        }
        else if (IsMemberOnParam(binary.Right, param))
        {
            memberExpr = (MemberExpression)binary.Right;
            valueExpr = binary.Left;
            flipped = true;
        }
        else
        {
            var result = (bool)ExpressionEvaluator.Evaluate(binary);
            return result
                ? new ComparisonFilter(new MemberAccess("__const", null), ExpressionType.Equal, true)
                : new ComparisonFilter(new MemberAccess("__const", null), ExpressionType.Equal, false);
        }

        var member = ResolveMemberAccess(memberExpr, param);
        var value = ExpressionEvaluator.Evaluate(valueExpr);
        var op = flipped ? FlipOperator(binary.NodeType) : binary.NodeType;

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
        var args = call.Arguments
            .Skip(call.Object is null ? 1 : 0)
            .Select(ExpressionEvaluator.Evaluate)
            .ToList();

        return call.Method.Name switch
        {
            "StartsWith" => new MethodFilter(member, "StartsWith", args),
            "EndsWith" => new MethodFilter(member, "EndsWith", args),
            "Contains" => new MethodFilter(member, "Contains", args),
            "Equals" => new ComparisonFilter(member, ExpressionType.Equal, args[0]),
            _ => throw new NotSupportedInQueryException(
                $"String method '{call.Method.Name}' is not supported in queries.")
        };
    }

    private MemberAccess ResolveMemberAccess(
        MemberExpression memberExpr, ParameterExpression param)
    {
        if (memberExpr.Expression != param)
            throw new NotSupportedInQueryException(
                $"Nested property access '{memberExpr}' is not supported. " +
                $"Only direct field access (e.g. c.Age) is supported.");

        var fieldName = memberExpr.Member.Name;
        var indexDef = _registry.FindByField(fieldName);
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
        ExpressionType.Equal or ExpressionType.NotEqual or
        ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or
        ExpressionType.LessThan or ExpressionType.LessThanOrEqual;

    private static ExpressionType FlipOperator(ExpressionType op) => op switch
    {
        ExpressionType.GreaterThan => ExpressionType.LessThan,
        ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
        ExpressionType.LessThan => ExpressionType.GreaterThan,
        ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
        _ => op
    };
}
