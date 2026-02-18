using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// A high-performance memory-mapped storage backend using unsafe pointers for direct memory access.
/// </summary>
/// <remarks>
/// This implementation uses unsafe code to get direct pointer access to the memory-mapped file,
/// providing the fastest possible page read/write performance. All unsafe operations are isolated
/// to a single method (GetPageSpan), with the rest of the class using safe Span&lt;T&gt; operations.
/// </remarks>
public sealed unsafe class MmapStorageBackend : IStorageBackend
{
    private const uint MagicNumber = 0x4D4D4146; // "MMAF" (Memory Mapped File)
    private const int HeaderMagicOffset = 0;
    private const int HeaderPageSizeOffset = 4;
    private const int HeaderPageCountOffset = 8;
    private const int HeaderFreeCountOffset = 16;
    private const int HeaderCapacityOffset = 24;
    private const int HeaderFreeListOffset = 32;
    private const long DefaultInitialPages = 1024;
    private const double GrowthFactor = 1.5;

    private readonly string _path;
    private readonly int _pageSize;
    private MemoryMappedFile _mmf = null!;
    private MemoryMappedViewAccessor _accessor = null!;
    private byte* _basePtr;
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

    public MmapStorageBackend(string path, long initialCapacityBytes = 0, int pageSize = 4096)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be positive.", nameof(pageSize));

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

        // Acquire the pointer for unsafe access
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _basePtr);
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
        byte[] header = new byte[_pageSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset), PageSize);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderPageCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderCapacityOffset), _capacityInPages);

        fixed (byte* headerPtr = header)
        {
            new Span<byte>(headerPtr, _pageSize).CopyTo(GetPageSpan(0));
        }
    }

    private void LoadHeader()
    {
        byte[] header = new byte[_pageSize];
        
        fixed (byte* headerPtr = header)
        {
            GetPageSpan(0).CopyTo(new Span<byte>(headerPtr, _pageSize));
        }

        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(HeaderMagicOffset));
        if (magic != MagicNumber)
            throw new InvalidDataException($"Invalid magic number: 0x{magic:X8}. Expected: 0x{MagicNumber:X8}");

        int storedPageSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset));
        if (storedPageSize != PageSize)
            throw new InvalidDataException($"Page size mismatch. File: {storedPageSize}, Requested: {PageSize}");

        _allocatedPages = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderPageCountOffset));
        long freeCount = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset));
        _capacityInPages = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderCapacityOffset));

        int maxFreeEntries = (int)Math.Min(freeCount, (_pageSize - HeaderFreeListOffset) / sizeof(long));
        for (int i = 0; i < maxFreeEntries; i++)
        {
            long freePageId = BinaryPrimitives.ReadInt64LittleEndian(
                header.AsSpan(HeaderFreeListOffset + i * sizeof(long)));
            _freePages.Push(freePageId);
        }

        _nextPageId = _allocatedPages;
    }

    private void SaveHeader()
    {
        byte[] header = new byte[_pageSize];
        
        // Read current header
        fixed (byte* headerPtr = header)
        {
            GetPageSpan(0).CopyTo(new Span<byte>(headerPtr, _pageSize));
        }

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

        fixed (byte* headerPtr = header)
        {
            new Span<byte>(headerPtr, _pageSize).CopyTo(GetPageSpan(0));
        }
    }

    /// <summary>
    /// Gets a Span pointing to the specified page in the memory-mapped file.
    /// This is the only method that uses unsafe code - the bridge between OS memory and Span world.
    /// </summary>
    private Span<byte> GetPageSpan(long pageId)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MmapStorageBackend));

        // +1 because header is at offset 0 (page 0)
        long offset = (pageId + 1) * _pageSize;
        
        if (offset < 0 || offset + _pageSize > _capacityInPages * _pageSize)
            throw new InvalidOperationException($"Page {pageId} offset {offset} exceeds capacity.");

        return new Span<byte>(_basePtr + offset, _pageSize);
    }

    public ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (pageId < 0 || pageId >= _allocatedPages)
            throw new PageNotFoundException(pageId);

        var buffer = new byte[_pageSize];
        GetPageSpan(pageId).CopyTo(buffer);
        return ValueTask.FromResult<Memory<byte>>(buffer);
    }

    public ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (data.Length != PageSize)
            throw new PageSizeMismatchException(PageSize, data.Length);

        if (pageId < 0 || pageId >= _allocatedPages)
            throw new PageNotFoundException(pageId);

        data.Span.CopyTo(GetPageSpan(pageId));
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
        // Release pointer before disposing
        _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        
        long newCapacity = (long)(_capacityInPages * GrowthFactor);
        if (newCapacity <= _capacityInPages)
            newCapacity = _capacityInPages + DefaultInitialPages;

        _capacityInPages = newCapacity;
        
        // Dispose and recreate with new capacity
        _accessor.Dispose();
        _mmf.Dispose();

        // Expand the file
        using (var fs = new FileStream(_path, FileMode.Open, FileAccess.Write))
        {
            fs.SetLength(_capacityInPages * _pageSize);
        }

        InitializeMemoryMappedFile();
        
        // Re-acquire pointer
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _basePtr);
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
            
            if (_basePtr != null)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                _basePtr = null;
            }
            
            _accessor.Dispose();
            _mmf.Dispose();
            _disposed = true;
        }

        return ValueTask.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MmapStorageBackend));
    }
}
