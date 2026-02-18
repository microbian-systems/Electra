namespace Aero.DataStructures.Trees.Persistence.Heap;

public static class HeapPageLayout
{
    public const byte NodeType = 0x03;

    public const int PageLsnOffset = 0;
    public const int PageVersionOffset = 8;
    public const int NodeTypeOffset = 12;
    public const int SlotCountOffset = 16;
    public const int LiveCountOffset = 18;
    public const int FreeSpaceOffset = 20;
    public const int HeaderSize = 32;

    public const int SlotEntrySize = 5;

    public const byte SlotLive = 0x00;
    public const byte SlotDeleted = 0x01;

    public static int SlotOffset(int slotIndex) =>
        HeaderSize + slotIndex * SlotEntrySize;

    public static int MaxSlots(int pageSize) =>
        (pageSize - HeaderSize) / (SlotEntrySize + 1);

    public static int FreeSpaceAvailable(int pageSize, int currentFreeSpaceOffset, int slotCount) =>
        pageSize - currentFreeSpaceOffset - (slotCount * SlotEntrySize);
}
