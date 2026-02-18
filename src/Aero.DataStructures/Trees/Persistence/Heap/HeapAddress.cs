using System;
using System.Runtime.InteropServices;

namespace Aero.DataStructures.Trees.Persistence.Heap;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct HeapAddress(long PageId, short SlotIndex)
    : IComparable<HeapAddress>
{
    public static readonly HeapAddress Null = new(-1, -1);
    public bool IsNull => PageId == -1;

    public int CompareTo(HeapAddress other)
    {
        var cmp = PageId.CompareTo(other.PageId);
        return cmp != 0 ? cmp : SlotIndex.CompareTo(other.SlotIndex);
    }
}
