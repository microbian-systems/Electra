using System.Runtime.InteropServices;

namespace Aero.DataStructures.Trees.Persistence.Nodes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BPlusLeafRecord<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    public RecordFlags Flags;
    public long XMin;
    public long XMax;
    public TKey Key;
    public TValue Value;

    public bool IsLive => (Flags & RecordFlags.Deleted) == 0 && XMax == 0;
    public bool IsDeleted => (Flags & RecordFlags.Deleted) != 0 || XMax != 0;

    public void MarkDeleted(long deleterTxnId)
    {
        XMax = deleterTxnId;
        Flags |= RecordFlags.Deleted;
        Value = default;
    }

    public void MarkDeleted()
    {
        Flags |= RecordFlags.Deleted;
        Value = default;
    }

    public static BPlusLeafRecord<TKey, TValue> Tombstone(TKey key) => new()
    {
        Flags = RecordFlags.Deleted,
        XMin = 0,
        XMax = 0,
        Key = key,
        Value = default
    };

    public static BPlusLeafRecord<TKey, TValue> Live(TKey key, TValue value) => new()
    {
        Flags = RecordFlags.None,
        XMin = 0,
        XMax = 0,
        Key = key,
        Value = value
    };

    public static BPlusLeafRecord<TKey, TValue> Create(TKey key, TValue value, long txnId) => new()
    {
        Flags = RecordFlags.None,
        XMin = txnId,
        XMax = 0,
        Key = key,
        Value = value
    };
}
