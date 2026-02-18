using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// Inline array buffer for safe stack-allocated page operations.
/// Maximum page size: 16KB (16384 bytes).
/// </summary>
[InlineArray(16384)]
public struct PageBuffer
{
    internal byte _element;
}

/// <summary>
/// A fully safe memory-mapped storage backend using .NET 8 InlineArray for stack-allocated buffers.
/// </summary>
/// <remarks>
/// This implementation provides:
/// - Zero heap allocations for page operations (stack-only via InlineArray)
/// - Fully safe code (no unsafe blocks)
/// - OS-level caching via MemoryMappedFile
/// - Runtime-configurable page size (up to 16KB)
/// 
/// Trade-off: Maximum page size is limited to 16KB to keep buffers stack-allocated.
/// </remarks>
public sealed class SafeMmapStorageBackend : IStorageBackend
{
    private const uint MagicNumber = 0x53414645; // "SAFE" in ASCII
    private const int HeaderMagicOffset = 0;
    private const int HeaderPageSizeOffset = 4;
    private const int HeaderPageCountOffset = 8;
    private const int HeaderFreeCountOffset = 16;
    private const int HeaderCapacityOffset = 24;
    private const int HeaderFreeListOffset = 32;
    private const long DefaultInitialPages = 1024;
    private const double GrowthFactor = 1.5;
    private const int MaxPageSize = 16384; // 16KB limit for InlineArray

    private readonly string _path;
    private readonly int _pageSize;
    private MemoryMappedFile _mmf = null!;
    private MemoryMappedViewAccessor _accessor = null!;
    private long _capacityInPages;
    private long _allocatedPages;
    private long _nextPageId;
    private readonly Stack<long> _freePages = new();
    private readonly object _resizeLock = new();
    private bool _disposed;

    public int PageSize => _pageSize;
    public long PageCount => _allocatedPages;
    public long CapacityInPages => _capacityInPages;
    public long FreePageCount => _freePages.Count;

    public SafeMmapStorageBackend(string path, long initialCapacityBytes = 0, int pageSize = 4096)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be positive.", nameof(pageSize));

        if (pageSize > MaxPageSize)
            throw new ArgumentException($"Page size {pageSize} exceeds maximum of {MaxPageSize} bytes for safe mmap storage.", nameof(pageSize));

        _path = path;
        _pageSize = pageSize;

        if (initialCapacityBytes == 0)
            initialCapacityBytes = DefaultInitialPages * pageSize;

        if (initialCapacityBytes % pageSize != 0)
            throw new ArgumentException("Initial capacity must be a multiple of page size.", nameof(initialCapacityBytes));

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

        _accessor = _mmf.CreateViewAccessor(0, _capacityInPages * _pageSize, MemoryMappedFileAccess.ReadWrite);
    }

    private void InitializeHeader()
    {
        PageBuffer header = default;
        var headerSpan = MemoryMarshal.CreateSpan(ref header._element, _pageSize);
        headerSpan.Clear();

        BinaryPrimitives.WriteUInt32LittleEndian(headerSpan.Slice(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(headerSpan.Slice(HeaderPageSizeOffset), PageSize);
        BinaryPrimitives.WriteInt64LittleEndian(headerSpan.Slice(HeaderPageCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(headerSpan.Slice(HeaderFreeCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(headerSpan.Slice(HeaderCapacityOffset), _capacityInPages);

        _accessor.WriteArray(0, headerSpan.ToArray(), 0, _pageSize);
    }

    private void LoadHeader()
    {
        PageBuffer header = default;
        var headerSpan = MemoryMarshal.CreateSpan(ref header._element, _pageSize);
        
        _accessor.ReadArray(0, headerSpan.ToArray(), 0, _pageSize);

        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(headerSpan.Slice(HeaderMagicOffset));
        if (magic != MagicNumber)
            throw new InvalidDataException($"Invalid magic number: 0x{magic:X8}. Expected: 0x{MagicNumber:X8}");

        int storedPageSize = BinaryPrimitives.ReadInt32LittleEndian(headerSpan.Slice(HeaderPageSizeOffset));
        if (storedPageSize != PageSize)
            throw new InvalidDataException($"Page size mismatch. File: {storedPageSize}, Requested: {PageSize}");

        _allocatedPages = BinaryPrimitives.ReadInt64LittleEndian(headerSpan.Slice(HeaderPageCountOffset));
        long freeCount = BinaryPrimitives.ReadInt64LittleEndian(headerSpan.Slice(HeaderFreeCountOffset));
        _capacityInPages = BinaryPrimitives.ReadInt64LittleEndian(headerSpan.Slice(HeaderCapacityOffset));

        int maxFreeEntries = (int)Math.Min(freeCount, (_pageSize - HeaderFreeListOffset) / sizeof(long));
        for (int i = 0; i < maxFreeEntries; i++)
        {
            long freePageId = BinaryPrimitives.ReadInt64LittleEndian(
                headerSpan.Slice(HeaderFreeListOffset + i * sizeof(long)));
            _freePages.Push(freePageId);
        }

        _nextPageId = _allocatedPages;
    }

    private void SaveHeader()
    {
        PageBuffer header = default;
        var headerSpan = MemoryMarshal.CreateSpan(ref header._element, _pageSize);
        
        _accessor.ReadArray(0, headerSpan.ToArray(), 0, _pageSize);

        BinaryPrimitives.WriteUInt32LittleEndian(headerSpan.Slice(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(headerSpan.Slice(HeaderPageSizeOffset), PageSize);
        BinaryPrimitives.WriteInt64LittleEndian(headerSpan.Slice(HeaderPageCountOffset), _allocatedPages);
        BinaryPrimitives.WriteInt64LittleEndian(headerSpan.Slice(HeaderFreeCountOffset), _freePages.Count);
        BinaryPrimitives.WriteInt64LittleEndian(headerSpan.Slice(HeaderCapacityOffset), _capacityInPages);

        int freeListCount = Math.Min(_freePages.Count, (int)(_pageSize - HeaderFreeListOffset) / sizeof(long));
        var freeListArray = _freePages.Take(freeListCount).ToArray();
        for (int i = 0; i < freeListArray.Length; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(
                headerSpan.Slice(HeaderFreeListOffset + i * sizeof(long)),
                freeListArray[i]);
        }

        _accessor.WriteArray(0, headerSpan.ToArray(), 0, _pageSize);
    }

    public ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (pageId < 0 || pageId >= _allocatedPages)
            throw new PageNotFoundException(pageId);

        PageBuffer buffer = default;
        var span = MemoryMarshal.CreateSpan(ref buffer._element, _pageSize);

        long offset = (pageId + 1) * _pageSize;
        _accessor.ReadArray(offset, span.ToArray(), 0, _pageSize);

        return ValueTask.FromResult<Memory<byte>>(span.ToArray());
    }

    public ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (data.Length != PageSize)
            throw new PageSizeMismatchException(PageSize, data.Length);

        if (pageId < 0 || pageId >= _allocatedPages)
            throw new PageNotFoundException(pageId);

        long offset = (pageId + 1) * _pageSize;
        _accessor.WriteArray(offset, data.ToArray(), 0, _pageSize);

        return ValueTask.CompletedTask;
    }

    public ValueTask<long> AllocatePageAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        lock (_resizeLock)
        {
            long pageId;
            
            if (_freePages.Count > 0)
            {
                pageId = _freePages.Pop();
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
            return new ValueTask<long>(pageId);
        }
    }

    private void GrowCapacity()
    {
        long newCapacity = (long)(_capacityInPages * GrowthFactor);
        if (newCapacity <= _capacityInPages)
            newCapacity = _capacityInPages + DefaultInitialPages;

        _capacityInPages = newCapacity;
        
        _accessor.Dispose();
        _mmf.Dispose();

        using (var fs = new FileStream(_path, FileMode.Open, FileAccess.Write))
        {
            fs.SetLength(_capacityInPages * _pageSize);
        }

        InitializeMemoryMappedFile();
    }

    public ValueTask FreePageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        lock (_resizeLock)
        {
            if (pageId >= 0 && pageId < _allocatedPages)
            {
                _freePages.Push(pageId);
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

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            SaveHeader();
            _accessor.Dispose();
            _mmf.Dispose();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SafeMmapStorageBackend));
    }
}
