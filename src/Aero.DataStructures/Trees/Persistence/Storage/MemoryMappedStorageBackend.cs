using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// A memory-mapped file storage backend that provides high-performance page-based storage.
/// Uses MemoryMappedFile for efficient random access with operating system-level caching.
/// This is the safe implementation without direct pointer access.
/// </summary>
/// <remarks>
/// This backend provides better performance than FileStorageBackend for workloads with:
/// - Frequent random page access
/// - Large files that don't fit entirely in memory
/// - Need for operating system-level page caching
/// 
/// For zero-copy access, use MmapStorageBackend instead.
/// </remarks>
public sealed class MemoryMappedStorageBackend : IStorageBackend
{
    private const uint MagicNumber = 0x4D4D4150; // MMAP in ASCII
    private const int HeaderMagicOffset = 0;
    private const int HeaderPageSizeOffset = 4;
    private const int HeaderPageCountOffset = 8;
    private const int HeaderFreeCountOffset = 16;
    private const int HeaderCapacityOffset = 24;
    private const int HeaderFreeListOffset = 32;
    private const long DefaultInitialPages = 1024; // 4MB with 4KB pages
    private const double GrowthFactor = 1.5;

    private readonly string _path;
    private readonly long _pageSize;
    private MemoryMappedFile _mmf = null!;
    private MemoryMappedViewAccessor _accessor = null!;
    private readonly object _resizeLock = new();
    
    private readonly Stack<long> _freePages = new();
    private readonly HashSet<long> _freePageSet = new();
    private readonly Dictionary<long, PageMetadata> _metadata = new();
    private long _nextPageId = 0;
    private long _capacityInPages;
    private long _allocatedPages = 0;
    private bool _disposed;

    public int PageSize => (int)_pageSize;
    public long PageCount => _allocatedPages;
    public long CapacityInPages => _capacityInPages;
    public long FreePageCount => _freePages.Count;

    public MemoryMappedStorageBackend(string path, long initialCapacityBytes = 0, int pageSize = 4096)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be positive.", nameof(pageSize));
        
        if (initialCapacityBytes > 0 && initialCapacityBytes % pageSize != 0)
            throw new ArgumentException("Initial capacity must be a multiple of page size.", nameof(initialCapacityBytes));

        _path = path;
        _pageSize = pageSize;
        
        if (initialCapacityBytes == 0)
            initialCapacityBytes = DefaultInitialPages * pageSize;
        
        _capacityInPages = initialCapacityBytes / pageSize;

        var fileExists = File.Exists(path);
        
        if (fileExists)
        {
            var fileInfo = new FileInfo(path);
            _capacityInPages = fileInfo.Length / pageSize;
            
            if (fileInfo.Length % pageSize != 0)
                throw new InvalidDataException($"File size {fileInfo.Length} is not a multiple of page size {pageSize}.");
            
            InitializeMemoryMappedFile();
            LoadHeader();
        }
        else
        {
            InitializeMemoryMappedFile();
            InitializeHeader();
        }
    }

    private void InitializeMemoryMappedFile()
    {
        _mmf = MemoryMappedFile.CreateFromFile(
            _path,
            FileMode.OpenOrCreate,
            mapName: null,
            capacity: _capacityInPages * _pageSize,
            access: MemoryMappedFileAccess.ReadWrite);
        
        _accessor = _mmf.CreateViewAccessor();
    }

    private void InitializeHeader()
    {
        byte[] header = new byte[_pageSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset), PageSize);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderPageCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderCapacityOffset), _capacityInPages);
        
        WriteHeader(header);
    }

    private void LoadHeader()
    {
        byte[] header = new byte[_pageSize];
        ReadHeader(header);
        
        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(HeaderMagicOffset));
        if (magic != MagicNumber)
            throw new InvalidDataException($"Invalid magic number. Expected: {MagicNumber:X8}");
        
        int storedPageSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset));
        if (storedPageSize != PageSize)
            throw new InvalidDataException($"Page size mismatch. File: {storedPageSize}, Requested: {PageSize}");
        
        _allocatedPages = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderPageCountOffset));
        long freeCount = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset));
        _capacityInPages = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderCapacityOffset));
        
        _freePageSet.Clear();
        int maxFreeEntries = (int)Math.Min(freeCount, (_pageSize - HeaderFreeListOffset) / sizeof(long));
        for (int i = 0; i < maxFreeEntries; i++)
        {
            long freePageId = BinaryPrimitives.ReadInt64LittleEndian(
                header.AsSpan(HeaderFreeListOffset + i * sizeof(long)));
            _freePages.Push(freePageId);
            _freePageSet.Add(freePageId);
        }
        
        _nextPageId = _allocatedPages;
        
        // Initialize metadata for existing pages
        _metadata.Clear();
        for (long pageId = 0; pageId < _allocatedPages; pageId++)
        {
            _metadata[pageId] = new PageMetadata(
                PageId: pageId,
                TotalSlots: 0,
                LiveSlots: 0,
                DeadSlots: 0,
                IsFree: _freePageSet.Contains(pageId));
        }
    }

    private void SaveHeader()
    {
        byte[] header = new byte[_pageSize];
        ReadHeader(header);
        
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset), PageSize);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderPageCountOffset), _allocatedPages);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset), _freePages.Count);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderCapacityOffset), _capacityInPages);
        
        int freeListCount = Math.Min(_freePages.Count, (int)(_pageSize - HeaderFreeListOffset) / sizeof(long));
        var freeListArray = _freePages.Take(freeListCount).ToArray();
        for (int i = 0; i < freeListArray.Length; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(
                header.AsSpan(HeaderFreeListOffset + i * sizeof(long)),
                freeListArray[i]);
        }
        
        WriteHeader(header);
    }

    private void ReadHeader(byte[] header) => _accessor.ReadArray(0, header, 0, (int)_pageSize);
    private void WriteHeader(byte[] header) => _accessor.WriteArray(0, header, 0, (int)_pageSize);

    public ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (pageId < 0 || pageId >= _allocatedPages || _freePageSet.Contains(pageId))
            throw new PageNotFoundException(pageId);
        
        long offset = (pageId + 1) * _pageSize;
        var buffer = new byte[_pageSize];
        _accessor.ReadArray(offset, buffer, 0, (int)_pageSize);
        
        return ValueTask.FromResult<Memory<byte>>(buffer);
    }

    public ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (data.Length != PageSize)
            throw new PageSizeMismatchException(PageSize, data.Length);
        
        if (pageId < 0 || pageId >= _allocatedPages || _freePageSet.Contains(pageId))
            throw new PageNotFoundException(pageId);
        
        long offset = (pageId + 1) * _pageSize;
        
        if (offset + _pageSize > _capacityInPages * _pageSize)
            throw new InvalidOperationException("Page offset exceeds file capacity.");
        
        _accessor.WriteArray(offset, data.ToArray(), 0, (int)_pageSize);
        return ValueTask.CompletedTask;
    }

    public ValueTask<long> AllocatePageAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        long pageId;
        
        lock (_resizeLock)
        {
            if (_freePages.Count > 0)
            {
                pageId = _freePages.Pop();
                _freePageSet.Remove(pageId);
            }
            else
            {
                pageId = _nextPageId++;
                
                if (pageId >= _capacityInPages)
                {
                    GrowCapacity();
                }
            }
            
            _allocatedPages = Math.Max(_allocatedPages, pageId + 1);
            
            // Initialize metadata
            _metadata[pageId] = new PageMetadata(pageId, 0, 0, 0, false);
        }
        
        return new ValueTask<long>(pageId);
    }

    private void GrowCapacity()
    {
        long newCapacity = (long)(_capacityInPages * GrowthFactor);
        if (newCapacity <= _capacityInPages)
            newCapacity = _capacityInPages + DefaultInitialPages;
        
        DisposeResources();
        _capacityInPages = newCapacity;
        InitializeMemoryMappedFile();
        SaveHeader();
    }

    public ValueTask FreePageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (pageId >= 0 && pageId < _allocatedPages)
        {
            lock (_resizeLock)
            {
                _freePages.Push(pageId);
                _freePageSet.Add(pageId);
                
                // Update metadata to mark as free
                if (_metadata.TryGetValue(pageId, out var meta))
                {
                    _metadata[pageId] = meta with { IsFree = true };
                }
                
                if (pageId == _allocatedPages - 1)
                    _allocatedPages--;
            }
        }
        
        return ValueTask.CompletedTask;
    }

    public ValueTask FlushAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        SaveHeader();
        _accessor.Flush();
        return ValueTask.CompletedTask;
    }

    public ValueTask<PageMetadata> GetPageMetadataAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (!_metadata.TryGetValue(pageId, out var meta))
            throw new PageNotFoundException(pageId);

        return new ValueTask<PageMetadata>(meta);
    }

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

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            SaveHeader();
            DisposeResources();
            _disposed = true;
        }
    }

    private void DisposeResources()
    {
        _accessor?.Dispose();
        _mmf?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MemoryMappedStorageBackend));
    }
}
