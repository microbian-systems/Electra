using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Documents;
using Aero.DataStructures.Trees.Persistence.Indexes;
using Aero.DataStructures.Trees.Persistence.Linq.Planning;

namespace Aero.DataStructures.Trees.Persistence.Linq;

public static class DocumentQueryableExtensions
{
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
            found = true;
        }

        return result;
    }

    public static async ValueTask<TDocument> SingleAsync<TDocument>(
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
            found = true;
        }

        if (!found)
            throw new InvalidOperationException("Sequence contains no elements.");

        return result!;
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
