using System;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Interfaces;

namespace Aero.DataStructures.Trees.Persistence.Indexes;

public sealed class SecondaryIndexUpdater<TDocument, TField> : IIndexUpdater<TDocument>
    where TDocument : class
    where TField : unmanaged, IComparable<TField>
{
    private readonly IOrderedKeyValueTree<CompositeKey<TField, Guid>, Guid> _index;
    private readonly Func<TDocument, TField> _extractor;

    public SecondaryIndexUpdater(
        IOrderedKeyValueTree<CompositeKey<TField, Guid>, Guid> index,
        Func<TDocument, TField> extractor)
    {
        _index = index;
        _extractor = extractor;
    }

    public async ValueTask OnInsertAsync(Guid id, TDocument doc, CancellationToken ct)
    {
        var key = _extractor(doc);
        await _index.InsertAsync(new CompositeKey<TField, Guid>(key, id), id, ct);
    }

    public async ValueTask OnUpdateAsync(
        Guid id, TDocument old, TDocument updated, CancellationToken ct)
    {
        var oldKey = _extractor(old);
        var newKey = _extractor(updated);

        if (EqualityComparer<TField>.Default.Equals(oldKey, newKey))
            return;

        await _index.DeleteAsync(new CompositeKey<TField, Guid>(oldKey, id), ct);
        await _index.InsertAsync(new CompositeKey<TField, Guid>(newKey, id), id, ct);
    }

    public async ValueTask OnDeleteAsync(Guid id, TDocument doc, CancellationToken ct)
    {
        var key = _extractor(doc);
        await _index.DeleteAsync(new CompositeKey<TField, Guid>(key, id), ct);
    }
}

public sealed class UniqueIndexUpdater<TDocument, TField> : IIndexUpdater<TDocument>
    where TDocument : class
    where TField : unmanaged, IComparable<TField>
{
    private readonly IOrderedKeyValueTree<TField, Guid> _index;
    private readonly Func<TDocument, TField> _extractor;
    private readonly string _indexName;

    public UniqueIndexUpdater(
        IOrderedKeyValueTree<TField, Guid> index,
        Func<TDocument, TField> extractor,
        string indexName)
    {
        _index = index;
        _extractor = extractor;
        _indexName = indexName;
    }

    public async ValueTask OnInsertAsync(Guid id, TDocument doc, CancellationToken ct)
    {
        var key = _extractor(doc);

        if (await _index.ContainsAsync(key, ct))
            throw new UniqueConstraintViolationException(_indexName, key.ToString()!);

        await _index.InsertAsync(key, id, ct);
    }

    public async ValueTask OnUpdateAsync(
        Guid id, TDocument old, TDocument updated, CancellationToken ct)
    {
        var oldKey = _extractor(old);
        var newKey = _extractor(updated);

        if (EqualityComparer<TField>.Default.Equals(oldKey, newKey))
            return;

        if (await _index.ContainsAsync(newKey, ct))
            throw new UniqueConstraintViolationException(_indexName, newKey.ToString()!);

        await _index.DeleteAsync(oldKey, ct);
        await _index.InsertAsync(newKey, id, ct);
    }

    public async ValueTask OnDeleteAsync(Guid id, TDocument doc, CancellationToken ct)
    {
        var key = _extractor(doc);
        await _index.DeleteAsync(key, ct);
    }
}

public sealed class UniqueConstraintViolationException : Exception
{
    public string IndexName { get; }
    public string Value { get; }

    public UniqueConstraintViolationException(string indexName, string value)
        : base($"Unique index '{indexName}' already contains value '{value}'.")
    {
        IndexName = indexName;
        Value = value;
    }
}
