using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Serialization;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Trees;

/// <summary>
/// Base class for persistent heap implementations.
/// Provides shared sift logic parameterized by a comparison function.
/// </summary>
/// <typeparam name="T">The type of elements.</typeparam>
public abstract class PersistentHeapBase<T> : IAsyncDisposable
    where T : unmanaged, IComparable<T>
{
    /// <summary>
    /// The metadata page ID.
    /// </summary>
    protected const long MetadataPageId = 0;

    /// <summary>
    /// Offset for count field in metadata page.
    /// </summary>
    protected const int CountOffset = 0;

    /// <summary>
    /// The storage backend.
    /// </summary>
    protected readonly IStorageBackend Storage;

    /// <summary>
    /// The serializer for elements.
    /// </summary>
    protected readonly INodeSerializer<T> Serializer;

    /// <summary>
    /// Buffer for page operations.
    /// </summary>
    protected readonly byte[] PageBuffer;

    /// <summary>
    /// Current element count.
    /// </summary>
    protected long Count;

    /// <summary>
    /// Comparison function determining heap ordering.
    /// </summary>
    protected readonly Func<T, T, bool> ShouldBubbleUp;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistentHeapBase{T}"/> class.
    /// </summary>
    /// <param name="storage">The storage backend.</param>
    /// <param name="serializer">The serializer.</param>
    /// <param name="shouldBubbleUp">Comparison function returning true if first argument should bubble up past second.</param>
    protected PersistentHeapBase(
        IStorageBackend storage,
        INodeSerializer<T> serializer,
        Func<T, T, bool> shouldBubbleUp)
    {
        Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        ShouldBubbleUp = shouldBubbleUp ?? throw new ArgumentNullException(nameof(shouldBubbleUp));

        if (serializer.SerializedSize > storage.PageSize)
            throw new ArgumentException("Serializer size exceeds page size.");

        PageBuffer = new byte[storage.PageSize];

        // Initialize metadata page if needed
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeAsync()
    {
        try
        {
            var metadata = await Storage.ReadPageAsync(MetadataPageId);
            Count = BinaryPrimitives.ReadInt64LittleEndian(metadata.Span.Slice(CountOffset));
        }
        catch (PageNotFoundException)
        {
            // First time initialization
            Count = 0;
            await SaveMetadataAsync();
        }
    }

    /// <summary>
    /// Saves the metadata (count) to the metadata page.
    /// </summary>
    protected async Task SaveMetadataAsync()
    {
        BinaryPrimitives.WriteInt64LittleEndian(PageBuffer.AsSpan(CountOffset), Count);
        await Storage.WritePageAsync(MetadataPageId, PageBuffer);
    }

    /// <summary>
    /// Sifts an element up from the specified index to maintain heap property.
    /// </summary>
    protected async Task SiftUpAsync(long index, CancellationToken ct)
    {
        while (index > 0)
        {
            long parentIndex = (index - 1) / 2;

            var currentPage = await Storage.ReadPageAsync(index + 1, ct);
            var parentPage = await Storage.ReadPageAsync(parentIndex + 1, ct);

            var currentValue = Serializer.Deserialize(currentPage.Span);
            var parentValue = Serializer.Deserialize(parentPage.Span);

            if (!ShouldBubbleUp(currentValue, parentValue))
                break;

            // Swap values
            Serializer.Serialize(currentValue, PageBuffer);
            await Storage.WritePageAsync(parentIndex + 1, PageBuffer, ct);

            Serializer.Serialize(parentValue, PageBuffer);
            await Storage.WritePageAsync(index + 1, PageBuffer, ct);

            index = parentIndex;
        }
    }

    /// <summary>
    /// Sifts an element down from the specified index to maintain heap property.
    /// </summary>
    protected async Task SiftDownAsync(long index, CancellationToken ct)
    {
        while (true)
        {
            long leftChild = 2 * index + 1;
            long rightChild = 2 * index + 2;
            long target = index;

            var indexPage = await Storage.ReadPageAsync(index + 1, ct);
            var indexValue = Serializer.Deserialize(indexPage.Span);

            if (leftChild < Count)
            {
                var leftPage = await Storage.ReadPageAsync(leftChild + 1, ct);
                var leftValue = Serializer.Deserialize(leftPage.Span);

                if (ShouldBubbleUp(leftValue, indexValue))
                    target = leftChild;
            }

            if (rightChild < Count)
            {
                var rightPage = await Storage.ReadPageAsync(rightChild + 1, ct);
                var rightValue = Serializer.Deserialize(rightPage.Span);

                var targetValue = target == index ? indexValue :
                    Serializer.Deserialize((await Storage.ReadPageAsync(target + 1, ct)).Span);

                if (ShouldBubbleUp(rightValue, targetValue))
                    target = rightChild;
            }

            if (target == index)
                break;

            // Swap values
            var targetPage = await Storage.ReadPageAsync(target + 1, ct);
            var targetValueActual = Serializer.Deserialize(targetPage.Span);

            Serializer.Serialize(indexValue, PageBuffer);
            await Storage.WritePageAsync(target + 1, PageBuffer, ct);

            Serializer.Serialize(targetValueActual, PageBuffer);
            await Storage.WritePageAsync(index + 1, PageBuffer, ct);

            index = target;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Storage.DisposeAsync();
            _disposed = true;
        }
    }

    /// <summary>
    /// Throws ObjectDisposedException if the heap has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }
}
