using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// An in-memory storage backend backed by a dictionary.
/// Thread safety is NOT guaranteed.
/// </summary>
public sealed class MemoryStorageBackend : IStorageBackend
{
    private readonly Dictionary<long, byte[]> _pages = new();
    private readonly Dictionary<long, PageMetadata> _metadata = new();
    private readonly Stack<long> _freePages = new();
    private long _nextPageId = 0;
    private bool _disposed;

    /// <inheritdoc />
    public int PageSize { get; }

    /// <inheritdoc />
    public long PageCount => _pages.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryStorageBackend"/> class.
    /// </summary>
    /// <param name="pageSize">The page size in bytes. Default is 4096.</param>
    public MemoryStorageBackend(int pageSize = 4096)
    {
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be positive.", nameof(pageSize));
        
        PageSize = pageSize;
    }

    /// <inheritdoc />
    public ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (!_pages.TryGetValue(pageId, out var page))
            throw new PageNotFoundException(pageId);
        
        return new ValueTask<Memory<byte>>(page.AsMemory());
    }

    /// <inheritdoc />
    public ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (data.Length != PageSize)
            throw new PageSizeMismatchException(PageSize, data.Length);
        
        if (!_pages.ContainsKey(pageId))
            throw new PageNotFoundException(pageId);
        
        var page = new byte[PageSize];
        data.CopyTo(page);
        _pages[pageId] = page;
        
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<long> AllocatePageAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        long pageId;
        bool isReused = false;
        
        if (_freePages.Count > 0)
        {
            pageId = _freePages.Pop();
            isReused = true;
        }
        else
        {
            pageId = _nextPageId++;
        }
        
        _pages[pageId] = new byte[PageSize];
        
        // Initialize metadata
        _metadata[pageId] = new PageMetadata(
            PageId: pageId,
            TotalSlots: 0,
            LiveSlots: 0,
            DeadSlots: 0,
            IsFree: false);
        
        return new ValueTask<long>(pageId);
    }

    /// <inheritdoc />
    public ValueTask FreePageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_pages.Remove(pageId))
        {
            _freePages.Push(pageId);
            
            // Mark as free in metadata
            if (_metadata.TryGetValue(pageId, out var meta))
            {
                _metadata[pageId] = meta with { IsFree = true };
            }
        }
        
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask FlushAsync(CancellationToken ct = default)
    {
        // No-op for memory storage
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<PageMetadata> GetPageMetadataAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (!_metadata.TryGetValue(pageId, out var meta))
            throw new PageNotFoundException(pageId);
        
        return new ValueTask<PageMetadata>(meta);
    }

    /// <inheritdoc />
    public ValueTask UpdatePageMetadataAsync(
        long pageId,
        int liveDelta,
        int deadDelta,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (!_metadata.TryGetValue(pageId, out var meta))
            throw new PageNotFoundException(pageId);
        
        _metadata[pageId] = new PageMetadata(
            PageId: pageId,
            TotalSlots: meta.TotalSlots,
            LiveSlots: Math.Max(0, meta.LiveSlots + liveDelta),
            DeadSlots: Math.Max(0, meta.DeadSlots + deadDelta),
            IsFree: meta.IsFree);
        
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<PageMetadata> GetFragmentedPagesAsync(
        double fragmentationThreshold,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var fragmented = _metadata.Values
            .Where(m => !m.IsFree && m.Fragmentation >= fragmentationThreshold)
            .OrderByDescending(m => m.Fragmentation);
        
        foreach (var meta in fragmented)
        {
            ct.ThrowIfCancellationRequested();
            yield return meta;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _pages.Clear();
            _metadata.Clear();
            _freePages.Clear();
            _disposed = true;
        }
        
        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryStorageBackend));
    }
}
