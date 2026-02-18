using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Heap;
using Aero.DataStructures.Trees.Persistence.Indexes;
using Aero.DataStructures.Trees.Persistence.Interfaces;
using Aero.DataStructures.Trees.Persistence.Serialization;
using Aero.DataStructures.Trees.Persistence.Wal;

namespace Aero.DataStructures.Trees.Persistence.Documents;

public sealed class DocumentCollection<TDocument> : IDocumentCollection<TDocument>
    where TDocument : class
{
    private readonly IWalStorageBackend _storage;
    private readonly IHeapFile _heap;
    private readonly IOrderedKeyValueTree<Guid, HeapAddress> _primaryIndex;
    private readonly DocumentIndexRegistry<TDocument> _indexRegistry;
    private readonly IDocumentSerializer<TDocument> _serializer;
    private readonly Func<TDocument, Guid> _idExtractor;
    private long _approximateCount;

    public long ApproximateCount => Interlocked.Read(ref _approximateCount);

    public DocumentCollection(
        IWalStorageBackend storage,
        IHeapFile heap,
        IOrderedKeyValueTree<Guid, HeapAddress> primaryIndex,
        DocumentIndexRegistry<TDocument> indexRegistry,
        IDocumentSerializer<TDocument> serializer,
        Func<TDocument, Guid> idExtractor)
    {
        _storage = storage;
        _heap = heap;
        _primaryIndex = primaryIndex;
        _indexRegistry = indexRegistry;
        _serializer = serializer;
        _idExtractor = idExtractor;
    }

    public async ValueTask<Guid> InsertAsync(TDocument document, CancellationToken ct = default)
    {
        await using var txn = await _storage.BeginTransactionAsync(ct);

        try
        {
            var id = _idExtractor(document);
            if (id == Guid.Empty) id = Guid.NewGuid();

            if (await _primaryIndex.ContainsAsync(id, ct))
                throw new DuplicateKeyException(id);

            var bytes = _serializer.Serialize(document);
            var address = await _heap.WriteAsync(bytes, ct);

            await _primaryIndex.InsertAsync(id, address, ct);

            foreach (var updater in _indexRegistry.AllUpdaters)
                await updater.OnInsertAsync(id, document, ct);

            await txn.CommitAsync(ct);
            Interlocked.Increment(ref _approximateCount);
            return id;
        }
        catch
        {
            await txn.RollbackAsync(ct);
            throw;
        }
    }

    public async ValueTask<TDocument?> FindAsync(Guid id, CancellationToken ct = default)
    {
        var address = await _primaryIndex.FindAsync(id, ct);
        if (address is null || address.Value.IsNull) return null;

        var bytes = await _heap.ReadAsync(address.Value, ct);
        return _serializer.Deserialize(bytes);
    }

    public async ValueTask<bool> UpdateAsync(
        Guid id, TDocument document, CancellationToken ct = default)
    {
        await using var txn = await _storage.BeginTransactionAsync(ct);

        try
        {
            var oldAddress = await _primaryIndex.FindAsync(id, ct);
            if (oldAddress is null || oldAddress.Value.IsNull) return false;

            var oldBytes = await _heap.ReadAsync(oldAddress.Value, ct);
            var oldDoc = _serializer.Deserialize(oldBytes);
            var newBytes = _serializer.Serialize(document);
            var newAddress = await _heap.UpdateAsync(oldAddress.Value, newBytes, ct);

            if (newAddress != oldAddress.Value)
                await _primaryIndex.UpdateAsync(id, newAddress, ct);

            foreach (var updater in _indexRegistry.AllUpdaters)
                await updater.OnUpdateAsync(id, oldDoc, document, ct);

            await txn.CommitAsync(ct);
            return true;
        }
        catch
        {
            await txn.RollbackAsync(ct);
            throw;
        }
    }

    public async ValueTask<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var txn = await _storage.BeginTransactionAsync(ct);

        try
        {
            var address = await _primaryIndex.FindAsync(id, ct);
            if (address is null || address.Value.IsNull) return false;

            var bytes = await _heap.ReadAsync(address.Value, ct);
            var oldDoc = _serializer.Deserialize(bytes);

            foreach (var updater in _indexRegistry.AllUpdaters)
                await updater.OnDeleteAsync(id, oldDoc, ct);

            await _primaryIndex.DeleteAsync(id, ct);
            await _heap.DeleteAsync(address.Value, ct);

            await txn.CommitAsync(ct);
            Interlocked.Decrement(ref _approximateCount);
            return true;
        }
        catch
        {
            await txn.RollbackAsync(ct);
            throw;
        }
    }

    public IQueryable<TDocument> AsQueryable()
    {
        throw new NotImplementedException("Use the LINQ provider from the Linq namespace.");
    }

    public async IAsyncEnumerable<TDocument> ScanIndexAsync<TField>(
        Expression<Func<TDocument, TField>> fieldSelector,
        TField from,
        TField to,
        [EnumeratorCancellation] CancellationToken ct = default)
        where TField : unmanaged, IComparable<TField>
    {
        var fieldName = GetFieldName(fieldSelector);
        var def = _indexRegistry.FindByField(fieldName)
                    ?? throw new ArgumentException(
                        $"Field '{fieldName}' is not indexed.", nameof(fieldSelector));

        var executor = _indexRegistry.GetExecutor(def);

        await foreach (var docId in executor.ScanRangeAsync(from, to, ct))
        {
            var doc = await FindAsync(docId, ct);
            if (doc is not null) yield return doc;
        }
    }

    public async IAsyncEnumerable<TDocument> ScanAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var (_, data) in _heap.ScanAllAsync(ct))
            yield return _serializer.Deserialize(data);
    }

    private static string GetFieldName<TField>(Expression<Func<TDocument, TField>> expr) =>
        ((MemberExpression)expr.Body).Member.Name;
}
