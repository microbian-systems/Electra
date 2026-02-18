using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees.Persistence.Heap;

public ref struct SlottedPage
{
    private readonly Span<byte> _page;
    private readonly int _pageSize;

    public SlottedPage(Span<byte> page)
    {
        _page = page;
        _pageSize = page.Length;
    }

    public ulong PageLsn
    {
        get => BinaryPrimitives.ReadUInt64LittleEndian(_page[HeapPageLayout.PageLsnOffset..]);
        set => BinaryPrimitives.WriteUInt64LittleEndian(_page[HeapPageLayout.PageLsnOffset..], value);
    }

    public ushort SlotCount
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_page[HeapPageLayout.SlotCountOffset..]);
        set => BinaryPrimitives.WriteUInt16LittleEndian(_page[HeapPageLayout.SlotCountOffset..], value);
    }

    public ushort LiveCount
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_page[HeapPageLayout.LiveCountOffset..]);
        set => BinaryPrimitives.WriteUInt16LittleEndian(_page[HeapPageLayout.LiveCountOffset..], value);
    }

    public ushort FreeSpaceStart
    {
        get => BinaryPrimitives.ReadUInt16LittleEndian(_page[HeapPageLayout.FreeSpaceOffset..]);
        set => BinaryPrimitives.WriteUInt16LittleEndian(_page[HeapPageLayout.FreeSpaceOffset..], value);
    }

    public int FreeBytes =>
        _pageSize - FreeSpaceStart - (SlotCount * HeapPageLayout.SlotEntrySize);

    public short WriteRecord(ReadOnlySpan<byte> data)
    {
        if (data.Length > FreeBytes)
            throw new InvalidOperationException(
                $"Insufficient space: need {data.Length}, have {FreeBytes}.");

        short slotIndex = FindDeadSlot();
        bool newSlot = slotIndex == -1;

        if (newSlot)
            slotIndex = (short)SlotCount;

        var dataOffset = (ushort)(_pageSize - FreeSpaceStart - data.Length);
        data.CopyTo(_page[dataOffset..]);

        var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(_page[slotOffset..], dataOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(_page[(slotOffset + 2)..], (ushort)data.Length);
        _page[slotOffset + 4] = HeapPageLayout.SlotLive;

        if (newSlot) SlotCount++;
        LiveCount++;
        FreeSpaceStart += (ushort)data.Length;

        return slotIndex;
    }

    public ReadOnlySpan<byte> ReadRecord(short slotIndex)
    {
        var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
        var flags = _page[slotOffset + 4];

        if (flags == HeapPageLayout.SlotDeleted)
            return ReadOnlySpan<byte>.Empty;

        var dataOffset = BinaryPrimitives.ReadUInt16LittleEndian(_page[slotOffset..]);
        var dataLength = BinaryPrimitives.ReadUInt16LittleEndian(_page[(slotOffset + 2)..]);

        return _page.Slice(dataOffset, dataLength);
    }

    public void DeleteRecord(short slotIndex)
    {
        var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
        _page[slotOffset + 4] = HeapPageLayout.SlotDeleted;
        LiveCount--;
    }

    public bool TryUpdateRecord(short slotIndex, ReadOnlySpan<byte> newData)
    {
        var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
        var dataLength = BinaryPrimitives.ReadUInt16LittleEndian(_page[(slotOffset + 2)..]);

        if (newData.Length > dataLength) return false;

        var dataOffset = BinaryPrimitives.ReadUInt16LittleEndian(_page[slotOffset..]);
        newData.CopyTo(_page[dataOffset..]);
        BinaryPrimitives.WriteUInt16LittleEndian(_page[(slotOffset + 2)..], (ushort)newData.Length);
        return true;
    }

    public int Compact()
    {
        var liveRecords = new List<(short SlotIndex, byte[] Data)>();

        for (short i = 0; i < SlotCount; i++)
        {
            var record = ReadRecord(i);
            if (!record.IsEmpty)
                liveRecords.Add((i, record.ToArray()));
        }

        var dataRegionStart = HeapPageLayout.HeaderSize + SlotCount * HeapPageLayout.SlotEntrySize;
        _page[dataRegionStart..].Clear();

        int freedBytes = FreeSpaceStart;

        FreeSpaceStart = 0;
        foreach (var (slotIndex, data) in liveRecords)
        {
            var dataOffset = (ushort)(_pageSize - FreeSpaceStart - data.Length);
            data.CopyTo(_page[dataOffset..]);

            var slotOffset = HeapPageLayout.SlotOffset(slotIndex);
            BinaryPrimitives.WriteUInt16LittleEndian(_page[slotOffset..], dataOffset);
            BinaryPrimitives.WriteUInt16LittleEndian(_page[(slotOffset + 2)..], (ushort)data.Length);

            FreeSpaceStart += (ushort)data.Length;
        }

        return freedBytes - FreeSpaceStart;
    }

    public static SlottedPage InitializePage(Span<byte> page)
    {
        page.Clear();
        page[HeapPageLayout.NodeTypeOffset] = HeapPageLayout.NodeType;
        var sp = new SlottedPage(page);
        sp.SlotCount = 0;
        sp.LiveCount = 0;
        sp.FreeSpaceStart = 0;
        return sp;
    }

    private short FindDeadSlot()
    {
        for (short i = 0; i < SlotCount; i++)
        {
            var slotOffset = HeapPageLayout.SlotOffset(i);
            if (_page[slotOffset + 4] == HeapPageLayout.SlotDeleted)
                return i;
        }
        return -1;
    }
}
