using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// A file-based storage backend with asynchronous I/O operations.
/// Persists data, free page list, and page metadata to disk.
/// </summary>
public sealed class FileStorageBackend : IStorageBackend
{
    private const uint MagicNumber = 0x54524545; // "TREE" in ASCII
    private const int HeaderMagicOffset = 0;
    private const int HeaderPageSizeOffset = 4;
    private const int HeaderPageCountOffset = 8;
    private const int HeaderFreeCountOffset = 16;
    private const int HeaderMetadataCountOffset = 24;
    private const int HeaderFreeListOffset = 32;
    
    // Metadata entry size: pageId(8) + totalSlots(4) + liveSlots(4) + deadSlots(4) = 20 bytes
    private const int MetadataEntrySize = sizeof(long) + sizeof(int) + sizeof(int) + sizeof(int);
    
    private readonly FileStream _stream;
    private readonly int _pageSize;
    private readonly int _maxFreeListEntries;
    private readonly Dictionary<long, PageMetadata> _metadata = new();
    private readonly HashSet<long> _freePageSet = new();
    private long[] _freeList = Array.Empty<long>();
    private int _freeListCount = 0;
    private long _pageCount = 0;
    private bool _disposed;

    /// <inheritdoc />
    public int PageSize => _pageSize;

    /// <inheritdoc />
    public long PageCount => _pageCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileStorageBackend"/> class.
    /// </summary>
    /// <param name="path">The file path for storage.</param>
    /// <param name="pageSize">The page size in bytes. Default is 4096.</param>
    public FileStorageBackend(string path, int pageSize = 4096)
    {
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be positive.", nameof(pageSize));
        
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        
        _pageSize = pageSize;
        
        // Calculate max entries: we need space for both free list and metadata in header
        // Free list entries are 8 bytes each, metadata entries are 20 bytes each
        // Reserve half for each
        var availableSpace = pageSize - HeaderFreeListOffset;
        _maxFreeListEntries = availableSpace / (sizeof(long) + MetadataEntrySize + 16); // rough estimate
        
        var fileExists = File.Exists(path);
        _stream = new FileStream(
            path, 
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: pageSize,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        
        if (fileExists)
        {
            LoadHeaderAsync().GetAwaiter().GetResult();
        }
        else
        {
            InitializeHeaderAsync().GetAwaiter().GetResult();
        }
    }

    private async Task InitializeHeaderAsync()
    {
        var header = new byte[_pageSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset), _pageSize);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderPageCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderMetadataCountOffset), 0);
        
        await WriteHeaderAsync(header);
    }

    private async Task LoadHeaderAsync()
    {
        var header = await ReadHeaderAsync();
        
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(HeaderMagicOffset));
        if (magic != MagicNumber)
            throw new InvalidDataException($"Invalid magic number: 0x{magic:X8}. Expected: 0x{MagicNumber:X8}");
        
        var storedPageSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset));
        if (storedPageSize != _pageSize)
            throw new InvalidDataException($"Page size mismatch. File: {storedPageSize}, Requested: {_pageSize}");
        
        _pageCount = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderPageCountOffset));
        _freeListCount = (int)BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset));
        var metadataCount = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderMetadataCountOffset));
        
        // Load free list
        _freeList = new long[Math.Max(_freeListCount, 16)];
        _freePageSet.Clear();
        for (int i = 0; i < _freeListCount; i++)
        {
            var pageId = BinaryPrimitives.ReadInt64LittleEndian(
                header.AsSpan(HeaderFreeListOffset + i * sizeof(long)));
            _freeList[i] = pageId;
            _freePageSet.Add(pageId);
        }
        
        // Load metadata entries
        var metadataOffset = HeaderFreeListOffset + _maxFreeListEntries * sizeof(long);
        _metadata.Clear();
        for (int i = 0; i < metadataCount; i++)
        {
            var offset = metadataOffset + i * MetadataEntrySize;
            var pageId = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(offset));
            var totalSlots = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(offset + 8));
            var liveSlots = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(offset + 12));
            var deadSlots = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(offset + 16));
            
            _metadata[pageId] = new PageMetadata(pageId, totalSlots, liveSlots, deadSlots, _freePageSet.Contains(pageId));
        }
    }

    private async Task<byte[]> ReadHeaderAsync()
    {
        var header = new byte[_pageSize];
        _stream.Position = 0;
        var read = await _stream.ReadAsync(header.AsMemory(0, _pageSize));
        if (read != _pageSize)
            throw new InvalidDataException("Failed to read complete header page.");
        return header;
    }

    private async Task WriteHeaderAsync(byte[] header)
    {
        _stream.Position = 0;
        await _stream.WriteAsync(header.AsMemory(0, _pageSize));
    }

    private async Task SaveHeaderAsync()
    {
        var header = new byte[_pageSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset), _pageSize);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderPageCountOffset), _pageCount);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset), _freeListCount);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderMetadataCountOffset), _metadata.Count);
        
        // Write free list
        for (int i = 0; i < _freeListCount; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(
                header.AsSpan(HeaderFreeListOffset + i * sizeof(long)), 
                _freeList[i]);
        }
        
        // Write metadata entries
        var metadataOffset = HeaderFreeListOffset + _maxFreeListEntries * sizeof(long);
        int metaIndex = 0;
        foreach (var meta in _metadata.Values)
        {
            var offset = metadataOffset + metaIndex * MetadataEntrySize;
            BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(offset), meta.PageId);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset + 8), meta.TotalSlots);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset + 12), meta.LiveSlots);
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset + 16), meta.DeadSlots);
            metaIndex++;
        }
        
        await WriteHeaderAsync(header);
    }

    /// <inheritdoc />
    public async ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (pageId < 0 || pageId >= _pageCount || _freePageSet.Contains(pageId))
            throw new PageNotFoundException(pageId);
        
        var buffer = new byte[_pageSize];
        _stream.Position = (pageId + 1) * _pageSize; // +1 because header is page 0 in file
        
        var read = await _stream.ReadAsync(buffer.AsMemory(0, _pageSize), ct);
        if (read != _pageSize)
            throw new InvalidDataException($"Failed to read complete page {pageId}.");
        
        return buffer;
    }

    /// <inheritdoc />
    public async ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (data.Length != _pageSize)
            throw new PageSizeMismatchException(_pageSize, data.Length);
        
        if (pageId < 0 || pageId >= _pageCount || _freePageSet.Contains(pageId))
            throw new PageNotFoundException(pageId);
        
        _stream.Position = (pageId + 1) * _pageSize;
        await _stream.WriteAsync(data, ct);
    }

    /// <inheritdoc />
    public ValueTask<long> AllocatePageAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        long pageId;
        if (_freeListCount > 0)
        {
            pageId = _freeList[--_freeListCount];
            _freePageSet.Remove(pageId);
            
            // Update metadata to mark as not free
            if (_metadata.TryGetValue(pageId, out var meta))
            {
                _metadata[pageId] = meta with { IsFree = false };
            }
        }
        else
        {
            pageId = _pageCount;
        }
        
        _pageCount++;
        
        // Initialize metadata for new page
        _metadata[pageId] = new PageMetadata(pageId, 0, 0, 0, false);
        
        return new ValueTask<long>(pageId);
    }

    /// <inheritdoc />
    public ValueTask FreePageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (_freeListCount < _maxFreeListEntries)
        {
            if (_freeListCount >= _freeList.Length)
            {
                Array.Resize(ref _freeList, _freeListCount + 16);
            }
            _freeList[_freeListCount++] = pageId;
            _freePageSet.Add(pageId);
            
            // Update metadata to mark as free
            if (_metadata.TryGetValue(pageId, out var meta))
            {
                _metadata[pageId] = meta with { IsFree = true };
            }
        }
        
        _pageCount--;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask FlushAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SaveHeaderAsync();
        await _stream.FlushAsync(ct);
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
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await FlushAsync();
            await _stream.DisposeAsync();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileStorageBackend));
    }
}
