using System;
using System.Collections.Generic;
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
        if (_freePages.Count > 0)
        {
            pageId = _freePages.Pop();
        }
        else
        {
            pageId = _nextPageId++;
        }
        
        _pages[pageId] = new byte[PageSize];
        return new ValueTask<long>(pageId);
    }

    /// <inheritdoc />
    public ValueTask FreePageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_pages.Remove(pageId))
        {
            _freePages.Push(pageId);
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
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _pages.Clear();
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
