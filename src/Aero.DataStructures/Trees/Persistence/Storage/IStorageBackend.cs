using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// Abstracts the physical storage medium for tree data structures.
/// All data is treated as raw pages of bytes with no knowledge of tree structure or serialization.
/// </summary>
public interface IStorageBackend : IAsyncDisposable
{
    /// <summary>
    /// Reads a page by its stable integer ID.
    /// </summary>
    /// <param name="pageId">The ID of the page to read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The page data as a memory buffer.</returns>
    /// <exception cref="PageNotFoundException">Thrown if the page ID does not exist.</exception>
    ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default);

    /// <summary>
    /// Writes data to an existing or newly allocated page.
    /// </summary>
    /// <param name="pageId">The ID of the page to write.</param>
    /// <param name="data">The data to write. Length must equal <see cref="PageSize"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="PageSizeMismatchException">Thrown if data length does not match page size.</exception>
    ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>
    /// Allocates a new page and returns its ID.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the newly allocated page.</returns>
    /// <remarks>The page is not written to — caller must follow with <see cref="WritePageAsync"/>.</remarks>
    ValueTask<long> AllocatePageAsync(CancellationToken ct = default);

    /// <summary>
    /// Marks a page as free for reuse.
    /// </summary>
    /// <param name="pageId">The ID of the page to free.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask FreePageAsync(long pageId, CancellationToken ct = default);

    /// <summary>
    /// Flushes any buffered writes to the underlying medium.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    ValueTask FlushAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns cached metadata for a page without reading its full contents.
    /// </summary>
    /// <param name="pageId">The ID of the page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The metadata for the specified page.</returns>
    /// <exception cref="PageNotFoundException">Thrown if the page ID does not exist.</exception>
    ValueTask<PageMetadata> GetPageMetadataAsync(long pageId, CancellationToken ct = default);

    /// <summary>
    /// Adjusts the live and dead slot counts for a page after a tree operation.
    /// Called by tree implementations after tombstoning or compacting records.
    /// </summary>
    /// <param name="pageId">The ID of the page to update.</param>
    /// <param name="liveDelta">Change in live slot count (may be negative).</param>
    /// <param name="deadDelta">Change in dead slot count (may be negative).</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask UpdatePageMetadataAsync(
        long pageId,
        int liveDelta,
        int deadDelta,
        CancellationToken ct = default);

    /// <summary>
    /// Lazily yields pages whose fragmentation ratio meets or exceeds the threshold.
    /// Ordered by fragmentation descending (most fragmented first).
    /// </summary>
    /// <param name="fragmentationThreshold">Minimum fragmentation ratio (0.0 = all non-free pages).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Pages at or above the fragmentation threshold, ordered by fragmentation descending.</returns>
    IAsyncEnumerable<PageMetadata> GetFragmentedPagesAsync(
        double fragmentationThreshold,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the fixed page size in bytes for this backend instance.
    /// </summary>
    int PageSize { get; }

    /// <summary>
    /// Gets the total number of currently allocated (non-freed) pages.
    /// </summary>
    long PageCount { get; }
}
