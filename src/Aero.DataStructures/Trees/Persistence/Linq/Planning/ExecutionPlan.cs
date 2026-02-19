using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Aero.DataStructures.Trees.Persistence.Documents;
using Aero.DataStructures.Trees.Persistence.Indexes;
using Aero.DataStructures.Trees.Persistence.Linq.Translation;

namespace Aero.DataStructures.Trees.Persistence.Linq.Planning;

public abstract class ExecutionPlan<TDocument>
    where TDocument : class
{
    public abstract IAsyncEnumerable<TDocument> ExecuteAsync(
        IDocumentCollection<TDocument> collection,
        CancellationToken ct);
}

public sealed class FullScanPlan<TDocument>(
    Func<TDocument, bool>? predicate,
    int? take,
    int? skip) : ExecutionPlan<TDocument>
    where TDocument : class
{
    public override async IAsyncEnumerable<TDocument> ExecuteAsync(
        IDocumentCollection<TDocument> collection,
        [EnumeratorCancellation] CancellationToken ct)
    {
        int skipped = 0;
        int taken = 0;

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

public sealed class IndexPointLookupPlan<TDocument>(
    IIndexExecutor<TDocument> executor,
    IndexScanSpec spec,
    Func<TDocument, bool>? residual,
    int? take,
    int? skip) : ExecutionPlan<TDocument>
    where TDocument : class
{
    public override async IAsyncEnumerable<TDocument> ExecuteAsync(
        IDocumentCollection<TDocument> collection,
        [EnumeratorCancellation] CancellationToken ct)
    {
        int skipped = 0;
        int taken = 0;

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

public sealed class IndexRangeScanPlan<TDocument>(
    IIndexExecutor<TDocument> executor,
    IndexScanSpec spec,
    Func<TDocument, bool>? residual,
    int? take,
    int? skip,
    IReadOnlyList<OrderClause> sortClauses) : ExecutionPlan<TDocument>
    where TDocument : class
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

        IEnumerable<TDocument> sorted = results;
        if (sortClauses.Any())
            sorted = ApplySort(results, sortClauses);

        if (skip.HasValue) sorted = sorted.Skip(skip.Value);
        if (take.HasValue) sorted = sorted.Take(take.Value);

        foreach (var doc in sorted)
            yield return doc;
    }

    private static IOrderedEnumerable<TDocument> ApplySort(
        List<TDocument> docs, IReadOnlyList<OrderClause> clauses)
    {
        IOrderedEnumerable<TDocument>? ordered = null;

        foreach (var clause in clauses)
        {
            var param = Expression.Parameter(typeof(TDocument));
            var member = Expression.PropertyOrField(param, clause.Member.FieldName);
            var lambda = Expression.Lambda(member, param);
            var compiled = lambda.Compile();

            Func<TDocument, object?> extractor = d => compiled.DynamicInvoke(d);

            ordered = ordered is null
                ? (clause.Descending
                    ? docs.OrderByDescending(extractor)
                    : docs.OrderBy(extractor))
                : (clause.Descending
                    ? ordered.ThenByDescending(extractor)
                    : ordered.ThenBy(extractor));
        }

        return ordered!;
    }
}
