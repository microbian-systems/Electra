using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// A file-based storage backend with asynchronous I/O operations.
/// Persists data and free page list to disk.
/// </summary>
public sealed class FileStorageBackend : IStorageBackend
{
    private const uint MagicNumber = 0x54524545; // "TREE" in ASCII
    private const int HeaderMagicOffset = 0;
    private const int HeaderPageSizeOffset = 4;
    private const int HeaderPageCountOffset = 8;
    private const int HeaderFreeCountOffset = 16;
    private const int HeaderFreeListOffset = 24;
    
    private readonly FileStream _stream;
    private readonly int _maxFreeListEntries;
    private long[] _freeList = Array.Empty<long>();
    private int _freeListCount = 0;
    private long _pageCount = 0;
    private bool _disposed;

    /// <inheritdoc />
    public int PageSize { get; }

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
        
        PageSize = pageSize;
        _maxFreeListEntries = (pageSize - HeaderFreeListOffset) / sizeof(long);
        
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
        var header = new byte[PageSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset), PageSize);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderPageCountOffset), 0);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset), 0);
        
        await WriteHeaderAsync(header);
    }

    private async Task LoadHeaderAsync()
    {
        var header = await ReadHeaderAsync();
        
        var magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(HeaderMagicOffset));
        if (magic != MagicNumber)
            throw new InvalidDataException($"Invalid magic number: 0x{magic:X8}. Expected: 0x{MagicNumber:X8}");
        
        var storedPageSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset));
        if (storedPageSize != PageSize)
            throw new InvalidDataException($"Page size mismatch. File: {storedPageSize}, Requested: {PageSize}");
        
        _pageCount = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderPageCountOffset));
        _freeListCount = (int)BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset));
        
        _freeList = new long[_freeListCount];
        for (int i = 0; i < _freeListCount; i++)
        {
            _freeList[i] = BinaryPrimitives.ReadInt64LittleEndian(
                header.AsSpan(HeaderFreeListOffset + i * sizeof(long)));
        }
    }

    private async Task<byte[]> ReadHeaderAsync()
    {
        var header = new byte[PageSize];
        _stream.Position = 0;
        var read = await _stream.ReadAsync(header.AsMemory(0, PageSize));
        if (read != PageSize)
            throw new InvalidDataException("Failed to read complete header page.");
        return header;
    }

    private async Task WriteHeaderAsync(byte[] header)
    {
        _stream.Position = 0;
        await _stream.WriteAsync(header.AsMemory(0, PageSize));
    }

    private async Task SaveHeaderAsync()
    {
        var header = new byte[PageSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(HeaderMagicOffset), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(HeaderPageSizeOffset), PageSize);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderPageCountOffset), _pageCount);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(HeaderFreeCountOffset), _freeListCount);
        
        for (int i = 0; i < _freeListCount; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(
                header.AsSpan(HeaderFreeListOffset + i * sizeof(long)), 
                _freeList[i]);
        }
        
        await WriteHeaderAsync(header);
    }

    /// <inheritdoc />
    public async ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (pageId < 0 || pageId >= _pageCount)
            throw new PageNotFoundException(pageId);
        
        var buffer = new byte[PageSize];
        _stream.Position = (pageId + 1) * PageSize; // +1 because header is page 0 in file
        
        var read = await _stream.ReadAsync(buffer.AsMemory(0, PageSize), ct);
        if (read != PageSize)
            throw new InvalidDataException($"Failed to read complete page {pageId}.");
        
        return buffer;
    }

    /// <inheritdoc />
    public async ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        if (data.Length != PageSize)
            throw new PageSizeMismatchException(PageSize, data.Length);
        
        if (pageId < 0 || pageId >= _pageCount)
            throw new PageNotFoundException(pageId);
        
        _stream.Position = (pageId + 1) * PageSize;
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
        }
        else
        {
            pageId = _pageCount;
        }
        
        _pageCount++;
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
