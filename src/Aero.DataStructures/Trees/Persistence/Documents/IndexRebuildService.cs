using System;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Heap;
using Aero.DataStructures.Trees.Persistence.Indexes;
using Aero.DataStructures.Trees.Persistence.Serialization;

namespace Aero.DataStructures.Trees.Persistence.Documents;

public sealed class IndexRebuildService<TDocument>
    where TDocument : class
{
    private readonly IDocumentCollection<TDocument> _collection;
    private readonly DocumentIndexRegistry<TDocument> _registry;
    private readonly IDocumentSerializer<TDocument> _serializer;
    private readonly IHeapFile _heap;

    public IndexRebuildService(
        IDocumentCollection<TDocument> collection,
        DocumentIndexRegistry<TDocument> registry,
        IDocumentSerializer<TDocument> serializer,
        IHeapFile heap)
    {
        _collection = collection;
        _registry = registry;
        _serializer = serializer;
        _heap = heap;
    }

    public async ValueTask RebuildAllAsync(
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        foreach (var index in _registry.AllIndexes)
            await RebuildIndexAsync(index.FieldName, progress, ct);
    }

    public async ValueTask RebuildIndexAsync(
        string fieldName,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var def = _registry.FindByField(fieldName)
                  ?? throw new ArgumentException($"No index found for field '{fieldName}'.");
        var updater = _registry.GetUpdater(fieldName)
                      ?? throw new ArgumentException($"No updater found for field '{fieldName}'.");

        int count = 0;

        await foreach (var (_, data) in _heap.ScanAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            var document = _serializer.Deserialize(data);
            var id = ExtractId(document);

            await updater.OnInsertAsync(id, document, ct);
            progress?.Report(++count);
        }
    }

    private Guid ExtractId(TDocument document)
    {
        var idProp = typeof(TDocument).GetProperty("Id");
        if (idProp is not null && idProp.PropertyType == typeof(Guid))
            return (Guid)idProp.GetValue(document)!;
        return Guid.NewGuid();
    }
}
