using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Aero.DataStructures.Trees.Persistence.Indexes;
using Aero.DataStructures.Trees.Persistence.Linq.Translation;

namespace Aero.DataStructures.Trees.Persistence.Linq.Planning;

public sealed class QueryPlanner<TDocument>
    where TDocument : class
{
    private readonly IDocumentIndexRegistry<TDocument> _registry;
    private readonly IQueryDiagnostics? _diagnostics;

    public QueryPlanner(
        IDocumentIndexRegistry<TDocument> registry,
        IQueryDiagnostics? diagnostics = null)
    {
        _registry = registry;
        _diagnostics = diagnostics;
    }

    public ExecutionPlan<TDocument> Plan(TranslatedQuery<TDocument> query)
    {
        var indexSpec = SelectBestIndex(query.Filters);

        if (indexSpec is null)
        {
            var predicate = BuildResidualPredicate(query.Filters);

            _diagnostics?.ReportFullScan(typeof(TDocument).Name, query);

            return new FullScanPlan<TDocument>(predicate, query.Take, query.Skip);
        }

        var (_, residualFilters) = SplitFilters(query.Filters, indexSpec);
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
                needsSort ? query.OrderBys : Array.Empty<Translation.OrderClause>());
    }

    private static IndexScanSpec? SelectBestIndex(IReadOnlyList<Translation.FilterClause> filters)
    {
        IndexScanSpec? best = null;

        foreach (var filter in FlattenAndClauses(filters))
        {
            if (filter is not Translation.ComparisonFilter cmp || !cmp.IsIndexSatisfiable)
                continue;

            var candidate = BuildScanSpec(cmp);
            if (candidate is null) continue;

            if (best is null) { best = candidate; continue; }
            if (!best.IsPoint && candidate.IsPoint) { best = candidate; continue; }
            if (best.IsPoint && candidate.IsPoint &&
                candidate.Index.IsUnique && !best.Index.IsUnique) { best = candidate; continue; }
        }

        return best;
    }

    private static IndexScanSpec? BuildScanSpec(Translation.ComparisonFilter cmp) =>
        cmp.Operator switch
        {
            ExpressionType.Equal =>
                new IndexScanSpec
                {
                    Index = cmp.Member.IndexDefinition!,
                    From = cmp.Value,
                    To = cmp.Value,
                    IsPoint = true,
                },

            ExpressionType.GreaterThan =>
                new IndexScanSpec
                {
                    Index = cmp.Member.IndexDefinition!,
                    From = cmp.Value,
                    IsPoint = false,
                    IncludeFrom = false,
                },

            ExpressionType.GreaterThanOrEqual =>
                new IndexScanSpec
                {
                    Index = cmp.Member.IndexDefinition!,
                    From = cmp.Value,
                    IsPoint = false,
                },

            ExpressionType.LessThan =>
                new IndexScanSpec
                {
                    Index = cmp.Member.IndexDefinition!,
                    To = cmp.Value,
                    IsPoint = false,
                    IncludeTo = false,
                },

            ExpressionType.LessThanOrEqual =>
                new IndexScanSpec
                {
                    Index = cmp.Member.IndexDefinition!,
                    To = cmp.Value,
                    IsPoint = false,
                },

            _ => null
        };

    private static (IndexScanSpec primary, IReadOnlyList<Translation.FilterClause> residual)
        SplitFilters(IReadOnlyList<Translation.FilterClause> filters, IndexScanSpec indexSpec)
    {
        var residual = new List<Translation.FilterClause>();

        foreach (var filter in filters)
        {
            if (filter is Translation.ComparisonFilter cmp &&
                cmp.Member.FieldName == indexSpec.Index.FieldName &&
                cmp.IsIndexSatisfiable)
                continue;

            residual.Add(filter);
        }

        return (indexSpec, residual);
    }

    private static Func<TDocument, bool>? BuildResidualPredicate(
        IReadOnlyList<Translation.FilterClause> filters)
    {
        if (!filters.Any()) return null;

        var param = Expression.Parameter(typeof(TDocument), "doc");
        var body = filters
            .Select(f => ClauseToExpression(f, param))
            .Aggregate(Expression.AndAlso);

        return Expression.Lambda<Func<TDocument, bool>>(body, param).Compile();
    }

    private static Expression ClauseToExpression(Translation.FilterClause clause, ParameterExpression param)
    {
        return clause switch
        {
            Translation.ComparisonFilter cmp =>
                BuildComparisonExpression(cmp, param),

            Translation.AndFilter and =>
                Expression.AndAlso(
                    ClauseToExpression(and.Left, param),
                    ClauseToExpression(and.Right, param)),

            Translation.OrFilter or =>
                Expression.OrElse(
                    ClauseToExpression(or.Left, param),
                    ClauseToExpression(or.Right, param)),

            Translation.NotFilter not =>
                Expression.Not(ClauseToExpression(not.Inner, param)),

            Translation.MethodFilter method =>
                BuildMethodExpression(method, param),

            _ => throw new NotSupportedInQueryException(
                $"Cannot build residual expression for {clause.GetType().Name}")
        };
    }

    private static Expression BuildComparisonExpression(
        Translation.ComparisonFilter cmp, ParameterExpression param)
    {
        var memberExpr = Expression.PropertyOrField(param, cmp.Member.FieldName);
        var valueExpr = Expression.Constant(
            Convert.ChangeType(cmp.Value, memberExpr.Type));

        return cmp.Operator switch
        {
            ExpressionType.Equal => Expression.Equal(memberExpr, valueExpr),
            ExpressionType.NotEqual => Expression.NotEqual(memberExpr, valueExpr),
            ExpressionType.GreaterThan => Expression.GreaterThan(memberExpr, valueExpr),
            ExpressionType.GreaterThanOrEqual => Expression.GreaterThanOrEqual(memberExpr, valueExpr),
            ExpressionType.LessThan => Expression.LessThan(memberExpr, valueExpr),
            ExpressionType.LessThanOrEqual => Expression.LessThanOrEqual(memberExpr, valueExpr),
            _ => throw new NotSupportedInQueryException(
                $"Operator {cmp.Operator} cannot be compiled to residual expression.")
        };
    }

    private static Expression BuildMethodExpression(
        Translation.MethodFilter method, ParameterExpression param)
    {
        var target = Expression.PropertyOrField(param, method.Target.FieldName);
        var stringArg = Expression.Constant(method.Arguments[0].ToString());
        var methodInfo = typeof(string).GetMethod(method.MethodName, new[] { typeof(string) })!;
        return Expression.Call(target, methodInfo, stringArg);
    }

    private static bool RequiresSortStep(
        IndexScanSpec indexSpec, IReadOnlyList<Translation.OrderClause> orderBys)
    {
        if (!orderBys.Any()) return false;
        var first = orderBys[0];
        return !(first.Member.FieldName == indexSpec.Index.FieldName &&
                 first.Descending == indexSpec.Index.IsDescending);
    }

    private static IEnumerable<Translation.FilterClause> FlattenAndClauses(
        IEnumerable<Translation.FilterClause> filters)
    {
        foreach (var f in filters)
        {
            if (f is Translation.AndFilter and)
            {
                foreach (var inner in FlattenAndClauses(new[] { and.Left, and.Right }))
                    yield return inner;
            }
            else yield return f;
        }
    }
}

public interface IQueryDiagnostics
{
    void ReportIndexScan(string collectionName, IndexScanSpec spec, bool hasResidual);
    void ReportFullScan(string collectionName, object query);
}
