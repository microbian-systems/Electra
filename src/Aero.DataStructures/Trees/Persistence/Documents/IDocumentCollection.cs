using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Heap;

namespace Aero.DataStructures.Trees.Persistence.Documents;

public interface IDocumentCollection<TDocument> where TDocument : class
{
    ValueTask<Guid> InsertAsync(
        TDocument document,
        CancellationToken ct = default);

    ValueTask<TDocument?> FindAsync(
        Guid id,
        CancellationToken ct = default);

    ValueTask<bool> UpdateAsync(
        Guid id,
        TDocument document,
        CancellationToken ct = default);

    ValueTask<bool> DeleteAsync(
        Guid id,
        CancellationToken ct = default);

    IQueryable<TDocument> AsQueryable();

    IAsyncEnumerable<TDocument> ScanIndexAsync<TField>(
        Expression<Func<TDocument, TField>> fieldSelector,
        TField from,
        TField to,
        CancellationToken ct = default)
        where TField : unmanaged, IComparable<TField>;

    IAsyncEnumerable<TDocument> ScanAllAsync(
        CancellationToken ct = default);

    long ApproximateCount { get; }
}
