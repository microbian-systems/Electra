using System;
using System.Buffers.Binary;
using Aero.DataStructures.Trees.Persistence.Wal;

namespace Aero.DataStructures.Trees.Persistence.Format;

public sealed class FileHeader
{
    public const uint MagicNumber = 0x54504446;
    public const ushort CurrentVersion = 1;
    public const int HeaderPageSize = 4096;

    private const int MagicOffset = 0;
    private const int FormatVersionOffset = 4;
    private const int PageSizeOffset = 6;
    private const int FileIdLowOffset = 8;
    private const int FileIdHighOffset = 16;
    private const int CreatedAtOffset = 24;
    private const int LastOpenedAtOffset = 32;
    private const int PageCountOffset = 40;
    private const int FreePageCountOffset = 48;
    private const int CatalogPageIdOffset = 56;
    private const int LastCheckpointLsnOffset = 64;
    private const int MinActiveTxnIdOffset = 72;
    private const int NextTransactionIdOffset = 80;
    private const int ShutdownStateOffset = 88;
    private const int ReservedOffset = 89;

    public ushort FormatVersion { get; init; }
    public int PageSize { get; init; }
    public Guid FileId { get; init; }
    public long CreatedAtUtc { get; init; }
    public long LastOpenedAtUtc { get; set; }
    public long PageCount { get; set; }
    public long FreePageCount { get; set; }
    public long CatalogPageId { get; init; } = 1;
    public Lsn LastCheckpointLsn { get; set; }
    public long MinActiveTxnId { get; set; }
    public long NextTransactionId { get; set; }
    public ShutdownState ShutdownState { get; set; }

    private FileHeader() { }

    public static FileHeader CreateNew(int pageSize)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new FileHeader
        {
            FormatVersion = CurrentVersion,
            PageSize = pageSize,
            FileId = Guid.NewGuid(),
            CreatedAtUtc = now,
            LastOpenedAtUtc = now,
            PageCount = 2,
            FreePageCount = 0,
            CatalogPageId = 1,
            LastCheckpointLsn = Lsn.Zero,
            MinActiveTxnId = 0,
            NextTransactionId = 1,
            ShutdownState = ShutdownState.Dirty,
        };
    }

    public static FileHeader ReadFrom(ReadOnlySpan<byte> page)
    {
        if (page.Length < HeaderPageSize)
            throw new ArgumentException($"Page must be at least {HeaderPageSize} bytes.", nameof(page));

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(page.Slice(MagicOffset, 4));
        if (magic != MagicNumber)
            throw new InvalidDataException("Invalid file: bad magic number.");

        var formatVersion = BinaryPrimitives.ReadUInt16LittleEndian(page.Slice(FormatVersionOffset, 2));
        if (formatVersion > CurrentVersion)
            throw new UnsupportedFormatVersionException(formatVersion, CurrentVersion);

        var pageSizeUnits = BinaryPrimitives.ReadUInt16LittleEndian(page.Slice(PageSizeOffset, 2));
        var pageSize = pageSizeUnits * 512;

        var fileIdLow = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(FileIdLowOffset, 8));
        var fileIdHigh = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(FileIdHighOffset, 8));
        var fileIdBytes = new byte[16];
        BitConverter.TryWriteBytes(fileIdBytes.AsSpan(0, 8), fileIdLow);
        BitConverter.TryWriteBytes(fileIdBytes.AsSpan(8, 8), fileIdHigh);
        var fileId = new Guid(fileIdBytes);

        var createdAt = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(CreatedAtOffset, 8));
        var lastOpenedAt = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(LastOpenedAtOffset, 8));
        var pageCount = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(PageCountOffset, 8));
        var freePageCount = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(FreePageCountOffset, 8));
        var catalogPageId = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(CatalogPageIdOffset, 8));
        var checkpointLsn = new Lsn(BinaryPrimitives.ReadUInt64LittleEndian(page.Slice(LastCheckpointLsnOffset, 8)));
        var minActiveTxnId = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(MinActiveTxnIdOffset, 8));
        var nextTransactionId = BinaryPrimitives.ReadInt64LittleEndian(page.Slice(NextTransactionIdOffset, 8));
        var shutdownState = (ShutdownState)page[ShutdownStateOffset];

        return new FileHeader
        {
            FormatVersion = formatVersion,
            PageSize = pageSize,
            FileId = fileId,
            CreatedAtUtc = createdAt,
            LastOpenedAtUtc = lastOpenedAt,
            PageCount = pageCount,
            FreePageCount = freePageCount,
            CatalogPageId = catalogPageId,
            LastCheckpointLsn = checkpointLsn,
            MinActiveTxnId = minActiveTxnId,
            NextTransactionId = nextTransactionId,
            ShutdownState = shutdownState,
        };
    }

    public void WriteTo(Span<byte> page)
    {
        if (page.Length < HeaderPageSize)
            throw new ArgumentException($"Page must be at least {HeaderPageSize} bytes.", nameof(page));

        page.Clear();

        BinaryPrimitives.WriteUInt32LittleEndian(page.Slice(MagicOffset, 4), MagicNumber);
        BinaryPrimitives.WriteUInt16LittleEndian(page.Slice(FormatVersionOffset, 2), FormatVersion);
        BinaryPrimitives.WriteUInt16LittleEndian(page.Slice(PageSizeOffset, 2), (ushort)(PageSize / 512));

        var fileIdBytes = FileId.ToByteArray();
        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(FileIdLowOffset, 8), 
            BitConverter.ToInt64(fileIdBytes, 0));
        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(FileIdHighOffset, 8), 
            BitConverter.ToInt64(fileIdBytes, 8));

        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(CreatedAtOffset, 8), CreatedAtUtc);
        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(LastOpenedAtOffset, 8), LastOpenedAtUtc);
        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(PageCountOffset, 8), PageCount);
        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(FreePageCountOffset, 8), FreePageCount);
        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(CatalogPageIdOffset, 8), CatalogPageId);
        BinaryPrimitives.WriteUInt64LittleEndian(page.Slice(LastCheckpointLsnOffset, 8), LastCheckpointLsn.Value);
        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(MinActiveTxnIdOffset, 8), MinActiveTxnId);
        BinaryPrimitives.WriteInt64LittleEndian(page.Slice(NextTransactionIdOffset, 8), NextTransactionId);
        page[ShutdownStateOffset] = (byte)ShutdownState;
    }
}
