using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Interfaces;

namespace Aero.DataStructures.Trees.Persistence.Indexes;

public sealed class DocumentIndexRegistry<TDocument> : IDocumentIndexRegistry<TDocument>
    where TDocument : class
{
    private readonly Dictionary<string, IndexDefinition> _byField = new();
    private readonly Dictionary<string, IIndexExecutor<TDocument>> _executors = new();
    private readonly Dictionary<string, IIndexUpdater<TDocument>> _updaters = new();
    private readonly List<IndexDefinition> _allIndexes = new();

    public IReadOnlyList<IndexDefinition> AllIndexes => _allIndexes;

    public IReadOnlyList<IIndexUpdater<TDocument>> AllUpdaters =>
        new List<IIndexUpdater<TDocument>>(_updaters.Values);

    public IndexDefinition? FindByField(string fieldName) =>
        _byField.TryGetValue(fieldName, out var def) ? def : null;

    public IIndexExecutor<TDocument> GetExecutor(IndexDefinition definition) =>
        _executors.TryGetValue(definition.Name, out var executor)
            ? executor
            : throw new InvalidOperationException($"No executor registered for index '{definition.Name}'.");

    public IIndexUpdater<TDocument>? GetUpdater(string fieldName) =>
        _updaters.TryGetValue(fieldName, out var updater) ? updater : null;

    public void Register<TField>(
        IndexDefinition<TDocument, TField> definition,
        IOrderedKeyValueTree<CompositeKey<TField, Guid>, Guid> tree)
        where TField : unmanaged, IComparable<TField>
    {
        _byField[definition.FieldName] = definition;
        _allIndexes.Add(definition);
        
        _executors[definition.Name] = new CompositeIndexExecutor<TDocument, TField>(definition, tree);
        _updaters[definition.FieldName] = new SecondaryIndexUpdater<TDocument, TField>(tree, definition.KeyExtractor);
    }

    public void RegisterUnique<TField>(
        IndexDefinition<TDocument, TField> definition,
        IOrderedKeyValueTree<TField, Guid> tree)
        where TField : unmanaged, IComparable<TField>
    {
        _byField[definition.FieldName] = definition;
        _allIndexes.Add(definition);
        
        _executors[definition.Name] = new UniqueIndexExecutor<TDocument, TField>(definition, tree);
        _updaters[definition.FieldName] = new UniqueIndexUpdater<TDocument, TField>(tree, definition.KeyExtractor, definition.Name);
    }
}

internal sealed class CompositeIndexExecutor<TDocument, TField> : IIndexExecutor<TDocument>
    where TDocument : class
    where TField : unmanaged, IComparable<TField>
{
    private readonly IndexDefinition _definition;
    private readonly IOrderedKeyValueTree<CompositeKey<TField, Guid>, Guid> _tree;

    public IndexDefinition Definition => _definition;

    public CompositeIndexExecutor(
        IndexDefinition definition,
        IOrderedKeyValueTree<CompositeKey<TField, Guid>, Guid> tree)
    {
        _definition = definition;
        _tree = tree;
    }

    public async IAsyncEnumerable<Guid> LookupAsync(
        object fieldValue,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var field = (TField)Convert.ChangeType(fieldValue, typeof(TField));
        var from = CompositeKey<TField, Guid>.RangeLo(field);
        var to = CompositeKey<TField, Guid>.RangeHi(field);

        await foreach (var key in _tree.ScanAsync(from, to, ct))
        {
            yield return key.Id;
        }
    }

    public async IAsyncEnumerable<Guid> ScanRangeAsync(
        object? from,
        object? to,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        TField fromField = from is null ? default : (TField)Convert.ChangeType(from, typeof(TField));
        TField toField = to is null ? GetMaxValue() : (TField)Convert.ChangeType(to, typeof(TField));
        
        var fromKey = CompositeKey<TField, Guid>.RangeLo(fromField);
        var toKey = CompositeKey<TField, Guid>.RangeHi(toField);

        await foreach (var key in _tree.ScanAsync(fromKey, toKey, ct))
        {
            yield return key.Id;
        }
    }

    private static TField GetMaxValue()
    {
        if (typeof(TField) == typeof(int)) return (TField)(object)int.MaxValue;
        if (typeof(TField) == typeof(long)) return (TField)(object)long.MaxValue;
        if (typeof(TField) == typeof(Guid)) return (TField)(object)new Guid(
            int.MaxValue, short.MaxValue, short.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        return default;
    }
}

internal sealed class UniqueIndexExecutor<TDocument, TField> : IIndexExecutor<TDocument>
    where TDocument : class
    where TField : unmanaged, IComparable<TField>
{
    private readonly IndexDefinition _definition;
    private readonly IOrderedKeyValueTree<TField, Guid> _tree;

    public IndexDefinition Definition => _definition;

    public UniqueIndexExecutor(
        IndexDefinition definition,
        IOrderedKeyValueTree<TField, Guid> tree)
    {
        _definition = definition;
        _tree = tree;
    }

    public async IAsyncEnumerable<Guid> LookupAsync(
        object fieldValue,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var field = (TField)Convert.ChangeType(fieldValue, typeof(TField));
        var (found, id) = await _tree.TryGetAsync(field, ct);
        if (found)
            yield return id;
    }

    public async IAsyncEnumerable<Guid> ScanRangeAsync(
        object? from,
        object? to,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        TField fromField = from is null ? default : (TField)Convert.ChangeType(from, typeof(TField));
        TField toField = to is null ? GetMaxValue() : (TField)Convert.ChangeType(to, typeof(TField));

        await foreach (var key in _tree.ScanAsync(fromField, toField, ct))
        {
            var (found, id) = await _tree.TryGetAsync(key, ct);
            if (found)
                yield return id;
        }
    }

    private static TField GetMaxValue()
    {
        if (typeof(TField) == typeof(int)) return (TField)(object)int.MaxValue;
        if (typeof(TField) == typeof(long)) return (TField)(object)long.MaxValue;
        if (typeof(TField) == typeof(Guid)) return (TField)(object)new Guid(
            int.MaxValue, short.MaxValue, short.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        return default;
    }
}
