using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Nodes;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Readers;

/// <summary>
/// Encapsulates the node read/write strategy for persistent B+ trees.
/// The implementation is chosen once at construction based on storage backend capabilities.
/// </summary>
/// <typeparam name="TKey">The type of keys, must be unmanaged and comparable.</typeparam>
/// <typeparam name="TValue">The type of values, must be unmanaged.</typeparam>
internal abstract class NodeReader<TKey, TValue>
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    /// <summary>Maximum capacity of records per leaf page.</summary>
    public int LeafCapacity { get; }
    
    /// <summary>Maximum degree (fan-out) for internal nodes.</summary>
    public int InternalDegree { get; }

    protected NodeReader(int leafCapacity, int internalDegree)
    {
        LeafCapacity = leafCapacity;
        InternalDegree = internalDegree;
    }

    /// <summary>Reads an internal node from the specified page.</summary>
    public abstract ValueTask<BPlusInternalNode<TKey>> ReadInternalAsync(long pageId, CancellationToken ct);

    /// <summary>Reads a leaf node from the specified page.</summary>
    public abstract ValueTask<BPlusLeafNode<TKey, TValue>> ReadLeafAsync(long pageId, CancellationToken ct);

    /// <summary>Writes an internal node to the specified page.</summary>
    public abstract ValueTask WriteInternalAsync(long pageId, BPlusInternalNode<TKey> node, CancellationToken ct);

    /// <summary>Writes a leaf node to the specified page.</summary>
    public abstract ValueTask WriteLeafAsync(long pageId, BPlusLeafNode<TKey, TValue> node, CancellationToken ct);

    /// <summary>
    /// Creates the appropriate NodeReader implementation based on storage backend capabilities.
    /// Uses ZeroCopyNodeReader if the backend supports IZeroCopyStorageBackend, otherwise CopyingNodeReader.
    /// </summary>
    public static NodeReader<TKey, TValue> Create(IStorageBackend storage)
    {
        var leafCapacity = BPlusLeafNode<TKey, TValue>.CalculateCapacity(storage.PageSize);
        var internalDegree = BPlusInternalNode<TKey>.CalculateDegree(storage.PageSize);
        
        return storage is IZeroCopyStorageBackend zc
            ? new ZeroCopyNodeReader<TKey, TValue>(zc, leafCapacity, internalDegree)
            : new CopyingNodeReader<TKey, TValue>(storage, leafCapacity, internalDegree);
    }
}

/// <summary>
/// Fast path node reader using zero-copy access directly into memory-mapped pages.
/// No allocation, no copying - direct mapped memory access.
/// </summary>
internal sealed class ZeroCopyNodeReader<TKey, TValue> : NodeReader<TKey, TValue>
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    private readonly IZeroCopyStorageBackend _storage;

    public ZeroCopyNodeReader(IZeroCopyStorageBackend storage, int leafCapacity, int internalDegree) 
        : base(leafCapacity, internalDegree)
    {
        _storage = storage;
    }

    public override ValueTask<BPlusInternalNode<TKey>> ReadInternalAsync(long pageId, CancellationToken ct)
    {
        ref var node = ref _storage.GetPageRef<BPlusInternalNode<TKey>>(pageId);
        return ValueTask.FromResult(node);
    }

    public override ValueTask<BPlusLeafNode<TKey, TValue>> ReadLeafAsync(long pageId, CancellationToken ct)
    {
        ref var node = ref _storage.GetPageRef<BPlusLeafNode<TKey, TValue>>(pageId);
        return ValueTask.FromResult(node);
    }

    public override ValueTask WriteInternalAsync(long pageId, BPlusInternalNode<TKey> node, CancellationToken ct)
    {
        _storage.GetPageRef<BPlusInternalNode<TKey>>(pageId) = node;
        return ValueTask.CompletedTask;
    }

    public override ValueTask WriteLeafAsync(long pageId, BPlusLeafNode<TKey, TValue> node, CancellationToken ct)
    {
        _storage.GetPageRef<BPlusLeafNode<TKey, TValue>>(pageId) = node;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Standard path node reader using async I/O with copies.
/// Works with any IStorageBackend implementation.
/// </summary>
internal sealed class CopyingNodeReader<TKey, TValue> : NodeReader<TKey, TValue>
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    private readonly IStorageBackend _storage;

    public CopyingNodeReader(IStorageBackend storage, int leafCapacity, int internalDegree) 
        : base(leafCapacity, internalDegree)
    {
        _storage = storage;
    }

    public override async ValueTask<BPlusInternalNode<TKey>> ReadInternalAsync(long pageId, CancellationToken ct)
    {
        var page = await _storage.ReadPageAsync(pageId, ct);
        return MemoryMarshal.AsRef<BPlusInternalNode<TKey>>(page.Span);
    }

    public override async ValueTask<BPlusLeafNode<TKey, TValue>> ReadLeafAsync(long pageId, CancellationToken ct)
    {
        var page = await _storage.ReadPageAsync(pageId, ct);
        return MemoryMarshal.AsRef<BPlusLeafNode<TKey, TValue>>(page.Span);
    }

    public override async ValueTask WriteInternalAsync(long pageId, BPlusInternalNode<TKey> node, CancellationToken ct)
    {
        var buffer = new byte[_storage.PageSize];
        MemoryMarshal.Write(buffer, ref node);
        await _storage.WritePageAsync(pageId, buffer, ct);
    }

    public override async ValueTask WriteLeafAsync(long pageId, BPlusLeafNode<TKey, TValue> node, CancellationToken ct)
    {
        var buffer = new byte[_storage.PageSize];
        MemoryMarshal.Write(buffer, ref node);
        await _storage.WritePageAsync(pageId, buffer, ct);
    }
}
