using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Interfaces;
using Aero.DataStructures.Trees.Persistence.Serialization;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Trees;

/// <summary>
/// A persistent min heap implementation that stores data in pages on a storage backend.
/// </summary>
/// <typeparam name="T">The type of elements. Must be unmanaged and comparable.</typeparam>
public class PersistentMinHeap<T> : IPriorityTree<T>, IAsyncDisposable
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
    /// Initializes a new instance of the <see cref="PersistentMinHeap{T}"/> class.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="serializer">The serializer for elements.</param>
    public PersistentMinHeap(IStorageBackend storage, INodeSerializer<T> serializer)
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
            // First time initialization
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
        
        // Allocate new page at the end
        long pageId = await _storage.AllocatePageAsync(ct);
        
        // Write value to page
        _serializer.Serialize(value, _pageBuffer);
        await _storage.WritePageAsync(pageId, _pageBuffer, ct);
        
        // Increment count
        _count++;
        await SaveMetadataAsync();
        
        // Sift up to maintain heap property
        await SiftUpAsync(_count - 1, ct);
    }

    /// <inheritdoc />
    public async ValueTask<T> PeekAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty.");
        
        // Root is always at page 1 (page 0 is metadata)
        var page = await _storage.ReadPageAsync(1, ct);
        return _serializer.Deserialize(page.Span);
    }

    /// <inheritdoc />
    public async ValueTask<T> ExtractAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty.");
        
        // Read root value (page 1)
        var rootPage = await _storage.ReadPageAsync(1, ct);
        var rootValue = _serializer.Deserialize(rootPage.Span);
        
        if (_count == 1)
        {
            // Only one element
            await _storage.FreePageAsync(1, ct);
            _count = 0;
            await SaveMetadataAsync();
        }
        else
        {
            // Read last element
            long lastPageId = _count; // Pages are 1-indexed for heap elements
            var lastPage = await _storage.ReadPageAsync(lastPageId, ct);
            var lastValue = _serializer.Deserialize(lastPage.Span);
            
            // Write last element to root
            _serializer.Serialize(lastValue, _pageBuffer);
            await _storage.WritePageAsync(1, _pageBuffer, ct);
            
            // Free last page
            await _storage.FreePageAsync(lastPageId, ct);
            
            // Decrement count
            _count--;
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
    public ValueTask<long> CountAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return new ValueTask<long>(_count);
    }

    /// <inheritdoc />
    public async ValueTask ClearAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        // Free all heap pages
        for (long i = 1; i <= _count; i++)
        {
            await _storage.FreePageAsync(i, ct);
        }
        
        _count = 0;
        await SaveMetadataAsync();
    }

    private async Task SiftUpAsync(long index, CancellationToken ct)
    {
        while (index > 0)
        {
            long parentIndex = (index - 1) / 2;
            
            var currentPage = await _storage.ReadPageAsync(index + 1, ct);
            var parentPage = await _storage.ReadPageAsync(parentIndex + 1, ct);
            
            var currentValue = _serializer.Deserialize(currentPage.Span);
            var parentValue = _serializer.Deserialize(parentPage.Span);
            
            if (currentValue.CompareTo(parentValue) >= 0)
                break;
            
            // Swap values
            _serializer.Serialize(currentValue, _pageBuffer);
            await _storage.WritePageAsync(parentIndex + 1, _pageBuffer, ct);
            
            _serializer.Serialize(parentValue, _pageBuffer);
            await _storage.WritePageAsync(index + 1, _pageBuffer, ct);
            
            index = parentIndex;
        }
    }

    private async Task SiftDownAsync(long index, CancellationToken ct)
    {
        while (true)
        {
            long leftChild = 2 * index + 1;
            long rightChild = 2 * index + 2;
            long smallest = index;
            
            var indexPage = await _storage.ReadPageAsync(index + 1, ct);
            var indexValue = _serializer.Deserialize(indexPage.Span);
            
            if (leftChild < _count)
            {
                var leftPage = await _storage.ReadPageAsync(leftChild + 1, ct);
                var leftValue = _serializer.Deserialize(leftPage.Span);
                
                if (leftValue.CompareTo(indexValue) < 0)
                    smallest = leftChild;
            }
            
            if (rightChild < _count)
            {
                var rightPage = await _storage.ReadPageAsync(rightChild + 1, ct);
                var rightValue = _serializer.Deserialize(rightPage.Span);
                
                var currentSmallestValue = smallest == index ? indexValue :
                    _serializer.Deserialize((await _storage.ReadPageAsync(smallest + 1, ct)).Span);

                if (rightValue.CompareTo(currentSmallestValue) < 0)
                    smallest = rightChild;
            }

            if (smallest == index)
                break;

            // Swap values
            var smallestPage = await _storage.ReadPageAsync(smallest + 1, ct);
            var smallestValue = _serializer.Deserialize(smallestPage.Span);
            
            _serializer.Serialize(indexValue, _pageBuffer);
            await _storage.WritePageAsync(smallest + 1, _pageBuffer, ct);
            
            _serializer.Serialize(smallestValue, _pageBuffer);
            await _storage.WritePageAsync(index + 1, _pageBuffer, ct);
            
            index = smallest;
        }
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
            throw new ObjectDisposedException(nameof(PersistentMinHeap<T>));
    }
}
