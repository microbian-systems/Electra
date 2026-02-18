using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Interfaces;

namespace Aero.DataStructures.Trees.Persistence.Indexes;

public interface IDocumentIndexRegistry<TDocument>
    where TDocument : class
{
    IndexDefinition? FindByField(string fieldName);
    IReadOnlyList<IndexDefinition> AllIndexes { get; }
    IIndexExecutor<TDocument> GetExecutor(IndexDefinition definition);
    
    void Register<TField>(
        IndexDefinition<TDocument, TField> definition,
        IOrderedKeyValueTree<CompositeKey<TField, Guid>, Guid> tree)
        where TField : unmanaged, IComparable<TField>;

    void RegisterUnique<TField>(
        IndexDefinition<TDocument, TField> definition,
        IOrderedKeyValueTree<TField, Guid> tree)
        where TField : unmanaged, IComparable<TField>;
}

public interface IIndexExecutor<TDocument>
    where TDocument : class
{
    IndexDefinition Definition { get; }
    
    IAsyncEnumerable<Guid> LookupAsync(
        object fieldValue,
        CancellationToken ct = default);
    
    IAsyncEnumerable<Guid> ScanRangeAsync(
        object? from,
        object? to,
        CancellationToken ct = default);
}
