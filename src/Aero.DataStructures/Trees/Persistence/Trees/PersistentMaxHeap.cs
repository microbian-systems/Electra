using System;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Interfaces;
using Aero.DataStructures.Trees.Persistence.Serialization;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Trees;

/// <summary>
/// A persistent max heap implementation that stores data in pages on a storage backend.
/// </summary>
/// <typeparam name="T">The type of elements. Must be unmanaged and comparable.</typeparam>
public class PersistentMaxHeap<T> : PersistentHeapBase<T>, IPriorityTree<T>
    where T : unmanaged, IComparable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentMaxHeap{T}"/> class.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="serializer">The serializer for elements.</param>
    public PersistentMaxHeap(IStorageBackend storage, INodeSerializer<T> serializer)
        : base(storage, serializer, (a, b) => a.CompareTo(b) > 0) // Max heap: bubble up if greater
    {
    }

    /// <inheritdoc />
    public async ValueTask InsertAsync(T value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        // Allocate new page at the end
        long pageId = await Storage.AllocatePageAsync(ct);
        
        // Write value to page
        Serializer.Serialize(value, PageBuffer);
        await Storage.WritePageAsync(pageId, PageBuffer, ct);
        
        // Increment count
        Count++;
        await SaveMetadataAsync();
        
        // Sift up to maintain heap property
        await SiftUpAsync(Count - 1, ct);
    }

    /// <inheritdoc />
    public async ValueTask<T> PeekAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (Count == 0)
            throw new InvalidOperationException("Heap is empty.");
        
        // Root is always at page 1 (page 0 is metadata)
        var page = await Storage.ReadPageAsync(1, ct);
        return Serializer.Deserialize(page.Span);
    }

    /// <inheritdoc />
    public async ValueTask<T> ExtractAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (Count == 0)
            throw new InvalidOperationException("Heap is empty.");
        
        // Read root value (page 1)
        var rootPage = await Storage.ReadPageAsync(1, ct);
        var rootValue = Serializer.Deserialize(rootPage.Span);
        
        if (Count == 1)
        {
            // Only one element
            await Storage.FreePageAsync(1, ct);
            Count = 0;
            await SaveMetadataAsync();
        }
        else
        {
            // Read last element
            long lastPageId = Count; // Pages are 1-indexed for heap elements
            var lastPage = await Storage.ReadPageAsync(lastPageId, ct);
            var lastValue = Serializer.Deserialize(lastPage.Span);
            
            // Write last element to root
            Serializer.Serialize(lastValue, PageBuffer);
            await Storage.WritePageAsync(1, PageBuffer, ct);
            
            // Free last page
            await Storage.FreePageAsync(lastPageId, ct);
            
            // Decrement count
            Count--;
            await SaveMetadataAsync();
            
            // Sift down from root
            await SiftDownAsync(0, ct);
        }
        
        return rootValue;
    }

    /// <inheritdoc />
    public async ValueTask<bool> ContainsAsync(T value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        // Linear search through all pages
        for (long i = 1; i <= Count; i++)
        {
            var page = await Storage.ReadPageAsync(i, ct);
            var element = Serializer.Deserialize(page.Span);
            if (element.Equals(value))
                return true;
        }
        
        return false;
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return new ValueTask<long>(Count);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        // Free all heap pages
        for (long i = 1; i <= Count; i++)
        {
            await Storage.FreePageAsync(i, ct);
        }
        
        Count = 0;
        await SaveMetadataAsync();
    }
}
