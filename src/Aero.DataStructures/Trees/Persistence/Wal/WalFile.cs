using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public sealed class WalFile : IWalWriter, IWalReader
{
    private const uint MagicNumber = 0x57414C21;
    private const int FormatVersion = 1;
    public const int HeaderSize = 24;

    private const int HeaderMagicOffset = 0;
    private const int HeaderVersionOffset = 4;
    private const int HeaderCheckpointOffset = 8;
    private const int HeaderNextLsnOffset = 16;

    private readonly string _path;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private FileStream _writeStream = null!;
    private FileStream _readStream = null!;
    private long _fileSize;
    private Lsn _nextLsn;
    private Lsn _lastCheckpointLsn;
    private bool _disposed;

    private readonly WalIndex _index = new();

    public Lsn NextLsn => _nextLsn;
    public long FileSize => _fileSize;
    public Lsn LastCheckpointLsn => _lastCheckpointLsn;

    public WalFile(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        Initialize();
    }

    private void Initialize()
    {
        var exists = File.Exists(_path);

        _writeStream = new FileStream(
            _path,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.Read,
            4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);

        _readStream = new FileStream(
            _path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        if (exists && _writeStream.Length >= HeaderSize)
        {
            LoadHeader();
            BuildIndex();
        }
        else
        {
            InitializeHeader();
        }

        _fileSize = _writeStream.Length;
    }

    private void LoadHeader()
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        _writeStream.Position = 0;
        _writeStream.ReadExactly(header);

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(HeaderMagicOffset, 4));
        if (magic != MagicNumber)
            throw new InvalidDataException("Invalid WAL file: bad magic number");

        var version = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(HeaderVersionOffset, 4));
        if (version != FormatVersion)
            throw new InvalidDataException($"Unsupported WAL version: {version}");

        var checkpointValue = BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(HeaderCheckpointOffset, 8));
        _lastCheckpointLsn = new Lsn(checkpointValue);

        var nextLsnValue = BinaryPrimitives.ReadUInt64LittleEndian(header.Slice(HeaderNextLsnOffset, 8));
        _nextLsn = new Lsn(nextLsnValue);
    }

    private void InitializeHeader()
    {
        _nextLsn = Lsn.MinValue;
        _lastCheckpointLsn = Lsn.Zero;

        Span<byte> header = stackalloc byte[HeaderSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(HeaderMagicOffset, 4), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.Slice(HeaderVersionOffset, 4), FormatVersion);
        BinaryPrimitives.WriteUInt64LittleEndian(header.Slice(HeaderCheckpointOffset, 8), _lastCheckpointLsn.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(header.Slice(HeaderNextLsnOffset, 8), _nextLsn.Value);

        _writeStream.Position = 0;
        _writeStream.Write(header);
        _writeStream.Flush();
    }

    private void SaveHeader()
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(HeaderMagicOffset, 4), MagicNumber);
        BinaryPrimitives.WriteInt32LittleEndian(header.Slice(HeaderVersionOffset, 4), FormatVersion);
        BinaryPrimitives.WriteUInt64LittleEndian(header.Slice(HeaderCheckpointOffset, 8), _lastCheckpointLsn.Value);
        BinaryPrimitives.WriteUInt64LittleEndian(header.Slice(HeaderNextLsnOffset, 8), _nextLsn.Value);

        _writeStream.Position = 0;
        _writeStream.Write(header);
    }

    private void BuildIndex()
    {
        if (_writeStream.Length <= HeaderSize)
            return;

        var position = HeaderSize;
        _writeStream.Position = position;

        Span<byte> headerBuffer = stackalloc byte[Marshal.SizeOf<WalEntryHeader>()];

        while (position < _writeStream.Length)
        {
            _writeStream.Position = position;

            var bytesRead = _writeStream.Read(headerBuffer);
            if (bytesRead < headerBuffer.Length)
                break;

            var header = MemoryMarshal.Read<WalEntryHeader>(headerBuffer);

            if (header.TotalLength <= 0 || header.TotalLength > 16 * 1024 * 1024)
                break;

            _index.Record(header.Lsn, position);

            position += header.TotalLength;
        }
    }

    public async ValueTask<Lsn> AppendAsync(WalEntry entry, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        await _writeLock.WaitAsync(ct);

        try
        {
            var lsn = _nextLsn;
            entry.Header.Lsn = lsn;

            var headerSize = Marshal.SizeOf<WalEntryHeader>();
            var payloadSize = entry.BeforeImage.Length + entry.AfterImage.Length;
            var totalSize = headerSize + payloadSize;

            entry.Header.TotalLength = totalSize;

            var buffer = ArrayPool<byte>.Shared.Rent(totalSize);

            try
            {
                var span = buffer.AsSpan(0, totalSize);

                MemoryMarshal.Write(span.Slice(0, headerSize), ref entry.Header);

                if (entry.BeforeImage.Length > 0)
                    entry.BeforeImage.Span.CopyTo(span.Slice(headerSize, entry.BeforeImage.Length));

                if (entry.AfterImage.Length > 0)
                    entry.AfterImage.Span.CopyTo(span.Slice(headerSize + entry.BeforeImage.Length, entry.AfterImage.Length));

                var crc = ComputeCrc32(span.Slice(4));
                BinaryPrimitives.WriteUInt32LittleEndian(span, crc);

                _writeStream.Position = _writeStream.Length;
                await _writeStream.WriteAsync(buffer.AsMemory(0, totalSize), ct);

                _index.Record(lsn, _fileSize);
                _fileSize += totalSize;
                _nextLsn = lsn.Next();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return lsn;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask FlushAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        await _writeLock.WaitAsync(ct);

        try
        {
            SaveHeader();
            await _writeStream.FlushAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async IAsyncEnumerable<WalEntry> ReadFromAsync(
        Lsn startLsn,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();

        long position = HeaderSize;

        if (_index.TryGetOffset(startLsn, out var indexOffset))
        {
            position = indexOffset;
        }

        var headerSize = Marshal.SizeOf<WalEntryHeader>();
        var headerBuffer = new byte[headerSize];

        while (position < _fileSize)
        {
            ct.ThrowIfCancellationRequested();

            _readStream.Position = position;

            var bytesRead = await _readStream.ReadAsync(headerBuffer, ct);
            if (bytesRead < headerSize)
                break;

            var header = MemoryMarshal.Read<WalEntryHeader>(headerBuffer);

            if (header.TotalLength <= 0 || header.TotalLength > 16 * 1024 * 1024)
                break;

            if (header.Lsn < startLsn)
            {
                position += header.TotalLength;
                continue;
            }

            var entryBuffer = ArrayPool<byte>.Shared.Rent(header.TotalLength);

            try
            {
                _readStream.Position = position;
                bytesRead = await _readStream.ReadAsync(entryBuffer.AsMemory(0, header.TotalLength), ct);

                if (bytesRead < header.TotalLength)
                    break;

                var span = entryBuffer.AsSpan(0, header.TotalLength);

                var storedCrc = BinaryPrimitives.ReadUInt32LittleEndian(span);
                var computedCrc = ComputeCrc32(span.Slice(4));

                if (storedCrc != computedCrc)
                    break;

                var entry = new WalEntry
                {
                    Header = header,
                };

                if (header.ImageLength > 0)
                {
                    var beforeSpan = span.Slice(headerSize, header.ImageLength);
                    var afterSpan = span.Slice(headerSize + header.ImageLength, header.ImageLength);

                    entry.BeforeImage = beforeSpan.ToArray();
                    entry.AfterImage = afterSpan.ToArray();
                }

                yield return entry;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(entryBuffer);
            }

            position += header.TotalLength;
        }
    }

    public IAsyncEnumerable<WalEntry> ReadAllAsync(CancellationToken ct = default)
    {
        return ReadFromAsync(Lsn.MinValue, ct);
    }

    public async ValueTask TruncateBeforeAsync(Lsn checkpointLsn, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        await _writeLock.WaitAsync(ct);

        try
        {
            if (!_index.TryGetOffset(checkpointLsn, out var checkpointOffset))
                return;

            var entries = new List<(long offset, WalEntry entry)>();

            await foreach (var entry in ReadFromAsync(checkpointLsn, ct))
            {
                if (_index.TryGetOffset(entry.Header.Lsn, out var offset))
                    entries.Add((offset, entry));
            }

            if (entries.Count == 0)
                return;

            var newContent = new List<byte>();
            newContent.AddRange(new byte[HeaderSize]);

            foreach (var (offset, entry) in entries)
            {
                var headerSize = Marshal.SizeOf<WalEntryHeader>();
                var payloadSize = entry.BeforeImage.Length + entry.AfterImage.Length;
                var totalSize = headerSize + payloadSize;

                var buffer = ArrayPool<byte>.Shared.Rent(totalSize);

                try
                {
                    var span = buffer.AsSpan(0, totalSize);

                    MemoryMarshal.Write(span.Slice(0, headerSize), ref entry.Header);

                    if (entry.BeforeImage.Length > 0)
                        entry.BeforeImage.Span.CopyTo(span.Slice(headerSize, entry.BeforeImage.Length));

                    if (entry.AfterImage.Length > 0)
                        entry.AfterImage.Span.CopyTo(span.Slice(headerSize + entry.BeforeImage.Length, entry.AfterImage.Length));

                    var crc = ComputeCrc32(span.Slice(4));
                    BinaryPrimitives.WriteUInt32LittleEndian(span, crc);

                    newContent.AddRange(span.ToArray());
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            _writeStream.SetLength(0);
            _writeStream.Position = 0;

            _lastCheckpointLsn = checkpointLsn;
            var headerSpan = newContent.ToArray().AsSpan(0, HeaderSize);
            BinaryPrimitives.WriteUInt32LittleEndian(headerSpan.Slice(HeaderMagicOffset, 4), MagicNumber);
            BinaryPrimitives.WriteInt32LittleEndian(headerSpan.Slice(HeaderVersionOffset, 4), FormatVersion);
            BinaryPrimitives.WriteUInt64LittleEndian(headerSpan.Slice(HeaderCheckpointOffset, 8), _lastCheckpointLsn.Value);
            BinaryPrimitives.WriteUInt64LittleEndian(headerSpan.Slice(HeaderNextLsnOffset, 8), _nextLsn.Value);

            await _writeStream.WriteAsync(newContent.ToArray(), ct);
            await _writeStream.FlushAsync(ct);

            _fileSize = _writeStream.Length;

            _index.TruncateBefore(checkpointLsn);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;

        foreach (var b in data)
        {
            crc ^= b;

            for (int i = 0; i < 8; i++)
            {
                crc = (crc >> 1) ^ (0xEDB88320 & (0 - (crc & 1)));
            }
        }

        return ~crc;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await _writeLock.WaitAsync();

        try
        {
            await _writeStream.DisposeAsync();
            await _readStream.DisposeAsync();
        }
        finally
        {
            _writeLock.Release();
            _writeLock.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WalFile));
    }

    private sealed class WalIndex : IWalIndex
    {
        private readonly Dictionary<ulong, long> _offsets = new();
        private Lsn _minLsn = Lsn.Zero;
        private Lsn _maxLsn = Lsn.Zero;

        public Lsn MinLsn => _minLsn;
        public Lsn MaxLsn => _maxLsn;

        public void Record(Lsn lsn, long fileOffset)
        {
            _offsets[lsn.Value] = fileOffset;

            if (_minLsn.IsNull || lsn < _minLsn)
                _minLsn = lsn;

            if (lsn > _maxLsn)
                _maxLsn = lsn;
        }

        public bool TryGetOffset(Lsn lsn, out long fileOffset)
        {
            return _offsets.TryGetValue(lsn.Value, out fileOffset);
        }

        public void TruncateBefore(Lsn lsn)
        {
            var toRemove = new List<ulong>();

            foreach (var kvp in _offsets)
            {
                if (new Lsn(kvp.Key) < lsn)
                    toRemove.Add(kvp.Key);
            }

            foreach (var key in toRemove)
                _offsets.Remove(key);

            if (_offsets.Count > 0)
            {
                _minLsn = lsn;
            }
            else
            {
                _minLsn = Lsn.Zero;
                _maxLsn = Lsn.Zero;
            }
        }
    }
}
