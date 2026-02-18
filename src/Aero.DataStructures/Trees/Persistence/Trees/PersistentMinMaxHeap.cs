using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Interfaces;
using Aero.DataStructures.Trees.Persistence.Serialization;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Trees;

/// <summary>
/// A persistent min-max heap implementation that provides O(1) access to both minimum and maximum elements.
/// </summary>
/// <remarks>
/// In a min-max heap:
/// - Even levels (0, 2, 4...) are "min levels" where nodes are smaller than their descendants
/// - Odd levels (1, 3, 5...) are "max levels" where nodes are larger than their descendants
/// </remarks>
/// <typeparam name="T">The type of elements. Must be unmanaged and comparable.</typeparam>
public class PersistentMinMaxHeap<T> : IDoubleEndedPriorityTree<T>, IAsyncDisposable
    where T : unmanaged, IComparable<T>
{
    private const long MetadataPageId = 0;
    private const int CountOffset = 0;
    
    private readonly IStorageBackend _storage;
    private readonly INodeSerializer<T> _serializer;
    private readonly byte[] _pageBuffer;
    private long _count;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentMinMaxHeap{T}"/> class.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="serializer">The serializer for elements.</param>
    public PersistentMinMaxHeap(IStorageBackend storage, INodeSerializer<T> serializer)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        
        if (serializer.SerializedSize > storage.PageSize)
            throw new ArgumentException("Serializer size exceeds page size.");
        
        _pageBuffer = new byte[storage.PageSize];
        
        // Initialize metadata page if needed
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var metadata = await _storage.ReadPageAsync(MetadataPageId);
            _count = BinaryPrimitives.ReadInt64LittleEndian(metadata.Span.Slice(CountOffset));
        }
        catch (PageNotFoundException)
        {
            _count = 0;
            await SaveMetadataAsync();
        }
    }

    private async Task SaveMetadataAsync()
    {
        BinaryPrimitives.WriteInt64LittleEndian(_pageBuffer.AsSpan(CountOffset), _count);
        await _storage.WritePageAsync(MetadataPageId, _pageBuffer);
    }

    /// <inheritdoc />
    public async ValueTask InsertAsync(T value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        long pageId = await _storage.AllocatePageAsync(ct);
        _serializer.Serialize(value, _pageBuffer);
        await _storage.WritePageAsync(pageId, _pageBuffer, ct);
        
        _count++;
        await SaveMetadataAsync();
        
        await SiftUpMinMaxAsync(_count - 1, ct);
    }

    /// <inheritdoc />
    public async ValueTask<T> PeekAsync(CancellationToken ct = default)
    {
        return await PeekMinAsync(ct);
    }

    /// <inheritdoc />
    public async ValueTask<T> PeekMinAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty.");
        
        var page = await _storage.ReadPageAsync(1, ct);
        return _serializer.Deserialize(page.Span);
    }

    /// <inheritdoc />
    public async ValueTask<T> PeekMaxAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty.");
        if (_count == 1)
            return await PeekMinAsync(ct);
        if (_count == 2)
        {
            var page = await _storage.ReadPageAsync(2, ct);
            return _serializer.Deserialize(page.Span);
        }
        
        // Return larger of the two children of root
        var leftPage = await _storage.ReadPageAsync(2, ct);
        var rightPage = await _storage.ReadPageAsync(3, ct);
        var leftValue = _serializer.Deserialize(leftPage.Span);
        var rightValue = _serializer.Deserialize(rightPage.Span);
        
        return leftValue.CompareTo(rightValue) > 0 ? leftValue : rightValue;
    }

    /// <inheritdoc />
    public async ValueTask<T> ExtractAsync(CancellationToken ct = default)
    {
        return await DeleteMinAsync(ct);
    }

    /// <inheritdoc />
    public async ValueTask<T> ExtractMaxAsync(CancellationToken ct = default)
    {
        return await DeleteMaxAsync(ct);
    }

    private async Task<T> DeleteMinAsync(CancellationToken ct)
    {
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty.");
        
        var rootPage = await _storage.ReadPageAsync(1, ct);
        var rootValue = _serializer.Deserialize(rootPage.Span);
        
        if (_count == 1)
        {
            await _storage.FreePageAsync(1, ct);
            _count = 0;
            await SaveMetadataAsync();
        }
        else
        {
            await RemoveAndReplaceAsync(0, ct);
        }
        
        return rootValue;
    }

    private async Task<T> DeleteMaxAsync(CancellationToken ct)
    {
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty.");
        if (_count == 1)
            return await DeleteMinAsync(ct);
        
        // Find max among root's children
        long maxIndex = 1;
        var maxPage = await _storage.ReadPageAsync(2, ct);
        var maxValue = _serializer.Deserialize(maxPage.Span);
        
        if (_count > 2)
        {
            var rightPage = await _storage.ReadPageAsync(3, ct);
            var rightValue = _serializer.Deserialize(rightPage.Span);
            if (rightValue.CompareTo(maxValue) > 0)
            {
                maxIndex = 2;
                maxValue = rightValue;
            }
        }
        
        await RemoveAndReplaceAsync(maxIndex, ct);
        return maxValue;
    }

    private async Task RemoveAndReplaceAsync(long index, CancellationToken ct)
    {
        long lastPageId = _count;
        var lastPage = await _storage.ReadPageAsync(lastPageId, ct);
        var lastValue = _serializer.Deserialize(lastPage.Span);
        
        _serializer.Serialize(lastValue, _pageBuffer);
        await _storage.WritePageAsync(index + 1, _pageBuffer, ct);
        await _storage.FreePageAsync(lastPageId, ct);
        
        _count--;
        await SaveMetadataAsync();
        
        if (_count > 0)
            await TrickleDownMinMaxAsync(index, ct);
    }

    private async Task SiftUpMinMaxAsync(long index, CancellationToken ct)
    {
        if (index == 0) return;
        
        long parentIndex = (index - 1) / 2;
        int level = GetLevel(index);
        
        var currentPage = await _storage.ReadPageAsync(index + 1, ct);
        var parentPage = await _storage.ReadPageAsync(parentIndex + 1, ct);
        var currentValue = _serializer.Deserialize(currentPage.Span);
        var parentValue = _serializer.Deserialize(parentPage.Span);
        
        if (IsMinLevel(level))
        {
            if (currentValue.CompareTo(parentValue) > 0)
            {
                SwapValues(index, parentIndex, currentValue, parentValue, ct).GetAwaiter().GetResult();
                SiftUpMaxAsync(parentIndex, ct).GetAwaiter().GetResult();
            }
            else
            {
                SiftUpMinAsync(index, ct).GetAwaiter().GetResult();
            }
        }
        else
        {
            if (currentValue.CompareTo(parentValue) < 0)
            {
                SwapValues(index, parentIndex, currentValue, parentValue, ct).GetAwaiter().GetResult();
                SiftUpMinAsync(parentIndex, ct).GetAwaiter().GetResult();
            }
            else
            {
                SiftUpMaxAsync(index, ct).GetAwaiter().GetResult();
            }
        }
    }

    private async Task SiftUpMinAsync(long index, CancellationToken ct)
    {
        if (index < 3) return; // No grandparent
        
        long grandparentIndex = (index - 3) / 4;
        var indexPage = await _storage.ReadPageAsync(index + 1, ct);
        var gpPage = await _storage.ReadPageAsync(grandparentIndex + 1, ct);
        var indexValue = _serializer.Deserialize(indexPage.Span);
        var gpValue = _serializer.Deserialize(gpPage.Span);
        
        if (indexValue.CompareTo(gpValue) < 0)
        {
            SwapValues(index, grandparentIndex, indexValue, gpValue, ct).GetAwaiter().GetResult();
            await SiftUpMinAsync(grandparentIndex, ct);
        }
    }

    private async Task SiftUpMaxAsync(long index, CancellationToken ct)
    {
        if (index < 3) return; // No grandparent
        
        long grandparentIndex = (index - 3) / 4;
        var indexPage = await _storage.ReadPageAsync(index + 1, ct);
        var gpPage = await _storage.ReadPageAsync(grandparentIndex + 1, ct);
        var indexValue = _serializer.Deserialize(indexPage.Span);
        var gpValue = _serializer.Deserialize(gpPage.Span);
        
        if (indexValue.CompareTo(gpValue) > 0)
        {
            SwapValues(index, grandparentIndex, indexValue, gpValue, ct).GetAwaiter().GetResult();
            await SiftUpMaxAsync(grandparentIndex, ct);
        }
    }

    private async Task TrickleDownMinMaxAsync(long index, CancellationToken ct)
    {
        int level = GetLevel(index);
        
        if (IsMinLevel(level))
            await TrickleDownMinAsync(index, ct);
        else
            await TrickleDownMaxAsync(index, ct);
    }

    private async Task TrickleDownMinAsync(long index, CancellationToken ct)
    {
        long minIndex = await FindMinDescendantAsync(index, ct);
        if (minIndex == -1) return;
        
        long parentOfMin = (minIndex - 1) / 2;
        var indexPage = await _storage.ReadPageAsync(index + 1, ct);
        var indexValue = _serializer.Deserialize(indexPage.Span);
        var minPage = await _storage.ReadPageAsync(minIndex + 1, ct);
        var minValue = _serializer.Deserialize(minPage.Span);
        
        if (minValue.CompareTo(indexValue) < 0)
        {
            SwapValues(minIndex, index, minValue, indexValue, ct).GetAwaiter().GetResult();
            
            if (parentOfMin != index && !IsMinLevel(GetLevel(minIndex)))
            {
                var parentPage = await _storage.ReadPageAsync(parentOfMin + 1, ct);
                var parentValue = _serializer.Deserialize(parentPage.Span);
                var currentMinPage = await _storage.ReadPageAsync(minIndex + 1, ct);
                var currentMinValue = _serializer.Deserialize(currentMinPage.Span);
                
                if (currentMinValue.CompareTo(parentValue) > 0)
                {
                    SwapValues(minIndex, parentOfMin, currentMinValue, parentValue, ct).GetAwaiter().GetResult();
                }
            }
            
            await TrickleDownMinAsync(minIndex, ct);
        }
    }

    private async Task TrickleDownMaxAsync(long index, CancellationToken ct)
    {
        long maxIndex = await FindMaxDescendantAsync(index, ct);
        if (maxIndex == -1) return;
        
        long parentOfMax = (maxIndex - 1) / 2;
        var indexPage = await _storage.ReadPageAsync(index + 1, ct);
        var indexValue = _serializer.Deserialize(indexPage.Span);
        var maxPage = await _storage.ReadPageAsync(maxIndex + 1, ct);
        var maxValue = _serializer.Deserialize(maxPage.Span);
        
        if (maxValue.CompareTo(indexValue) > 0)
        {
            SwapValues(maxIndex, index, maxValue, indexValue, ct).GetAwaiter().GetResult();
            
            if (parentOfMax != index && IsMinLevel(GetLevel(maxIndex)))
            {
                var parentPage = await _storage.ReadPageAsync(parentOfMax + 1, ct);
                var parentValue = _serializer.Deserialize(parentPage.Span);
                var currentMaxPage = await _storage.ReadPageAsync(maxIndex + 1, ct);
                var currentMaxValue = _serializer.Deserialize(currentMaxPage.Span);
                
                if (currentMaxValue.CompareTo(parentValue) < 0)
                {
                    SwapValues(maxIndex, parentOfMax, currentMaxValue, parentValue, ct).GetAwaiter().GetResult();
                }
            }
            
            await TrickleDownMaxAsync(maxIndex, ct);
        }
    }

    private async Task<long> FindMinDescendantAsync(long index, CancellationToken ct)
    {
        long leftChild = 2 * index + 1;
        long rightChild = 2 * index + 2;
        long leftGc1 = 2 * leftChild + 1;
        long leftGc2 = 2 * leftChild + 2;
        long rightGc1 = 2 * rightChild + 1;
        long rightGc2 = 2 * rightChild + 2;
        
        long minIndex = -1;
        T minValue = default;
        bool hasMin = false;
        
        var candidates = new[] { leftChild, rightChild, leftGc1, leftGc2, rightGc1, rightGc2 };
        
        foreach (var candidate in candidates)
        {
            if (candidate < _count)
            {
                var page = await _storage.ReadPageAsync(candidate + 1, ct);
                var value = _serializer.Deserialize(page.Span);
                
                if (!hasMin || value.CompareTo(minValue) < 0)
                {
                    minIndex = candidate;
                    minValue = value;
                    hasMin = true;
                }
            }
        }
        
        return minIndex;
    }

    private async Task<long> FindMaxDescendantAsync(long index, CancellationToken ct)
    {
        long leftChild = 2 * index + 1;
        long rightChild = 2 * index + 2;
        long leftGc1 = 2 * leftChild + 1;
        long leftGc2 = 2 * leftChild + 2;
        long rightGc1 = 2 * rightChild + 1;
        long rightGc2 = 2 * rightChild + 2;
        
        long maxIndex = -1;
        T maxValue = default;
        bool hasMax = false;
        
        var candidates = new[] { leftChild, rightChild, leftGc1, leftGc2, rightGc1, rightGc2 };
        
        foreach (var candidate in candidates)
        {
            if (candidate < _count)
            {
                var page = await _storage.ReadPageAsync(candidate + 1, ct);
                var value = _serializer.Deserialize(page.Span);
                
                if (!hasMax || value.CompareTo(maxValue) > 0)
                {
                    maxIndex = candidate;
                    maxValue = value;
                    hasMax = true;
                }
            }
        }
        
        return maxIndex;
    }

    private async Task SwapValues(long i, long j, T valueI, T valueJ, CancellationToken ct)
    {
        _serializer.Serialize(valueI, _pageBuffer);
        await _storage.WritePageAsync(j + 1, _pageBuffer, ct);
        
        _serializer.Serialize(valueJ, _pageBuffer);
        await _storage.WritePageAsync(i + 1, _pageBuffer, ct);
    }

    private int GetLevel(long index)
    {
        return (int)Math.Floor(Math.Log2(index + 1));
    }

    private bool IsMinLevel(int level)
    {
        return level % 2 == 0;
    }

    /// <inheritdoc />
    public async ValueTask<bool> ContainsAsync(T value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        for (long i = 1; i <= _count; i++)
        {
            var page = await _storage.ReadPageAsync(i, ct);
            var element = _serializer.Deserialize(page.Span);
            if (element.Equals(value))
                return true;
        }
        
        return false;
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAsync(T value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_count == 0)
            return false;
        
        // O(n) scan to find the value
        long foundIndex = -1;
        for (long i = 0; i < _count; i++)
        {
            var page = await _storage.ReadPageAsync(i + 1, ct);
            var element = _serializer.Deserialize(page.Span);
            
            if (element.Equals(value))
            {
                foundIndex = i;
                break;
            }
        }
        
        if (foundIndex == -1)
            return false;
        
        // Read last element
        var lastPage = await _storage.ReadPageAsync(_count, ct);
        var lastValue = _serializer.Deserialize(lastPage.Span);
        
        // Write last element into found index
        _serializer.Serialize(lastValue, _pageBuffer);
        await _storage.WritePageAsync(foundIndex + 1, _pageBuffer, ct);
        
        // Free the last page
        await _storage.FreePageAsync(_count, ct);
        
        // Decrement count
        _count--;
        await SaveMetadataAsync();
        
        if (_count > 0)
        {
            // Both SiftUp and TrickleDown are needed because replacing with the last
            // element could violate heap property in either direction
            await SiftUpMinMaxAsync(foundIndex, ct);
            await TrickleDownMinMaxAsync(foundIndex, ct);
        }
        
        return true;
    }

    /// <inheritdoc />
    public ValueTask<long> CountAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return new ValueTask<long>(_count);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        for (long i = 1; i <= _count; i++)
        {
            await _storage.FreePageAsync(i, ct);
        }
        
        _count = 0;
        await SaveMetadataAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _storage.DisposeAsync();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PersistentMinMaxHeap<T>));
    }
}
