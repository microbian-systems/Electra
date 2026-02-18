using System.Runtime.InteropServices;

namespace Aero.DataStructures.Trees.Persistence.Nodes;

/// <summary>
/// A single record slot within a B+ tree leaf page.
/// Blittable — safe to use with MemoryMarshal for direct memory mapping.
/// </summary>
/// <typeparam name="TKey">The type of the key. Must be unmanaged.</typeparam>
/// <typeparam name="TValue">The type of the value. Must be unmanaged.</typeparam>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BPlusLeafRecord<TKey, TValue>
    where TKey : unmanaged
    where TValue : unmanaged
{
    /// <summary>
    /// Flags indicating the status of this record (live, deleted, overflow).
    /// </summary>
    public RecordFlags Flags;
    
    /// <summary>
    /// The key for this record.
    /// </summary>
    public TKey Key;
    
    /// <summary>
    /// The value associated with this key.
    /// Cleared when the record is tombstoned.
    /// </summary>
    public TValue Value;

    /// <summary>
    /// Returns true if this record is not deleted.
    /// </summary>
    public bool IsLive => (Flags & RecordFlags.Deleted) == 0;
    
    /// <summary>
    /// Returns true if this record has been tombstoned.
    /// </summary>
    public bool IsDeleted => (Flags & RecordFlags.Deleted) != 0;

    /// <summary>
    /// Marks this record as deleted (tombstone).
    /// Clears the value to prevent data leakage.
    /// </summary>
    public void MarkDeleted()
    {
        Flags |= RecordFlags.Deleted;
        Value = default; // Clear value on tombstone — no data leakage
    }

    /// <summary>
    /// Creates a new tombstone record with the specified key.
    /// </summary>
    /// <param name="key">The key to retain in the tombstone.</param>
    /// <returns>A new record marked as deleted.</returns>
    public static BPlusLeafRecord<TKey, TValue> Tombstone(TKey key) => new()
    {
        Flags = RecordFlags.Deleted,
        Key = key,
        Value = default
    };

    /// <summary>
    /// Creates a new live record with the specified key and value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <returns>A new live record.</returns>
    public static BPlusLeafRecord<TKey, TValue> Live(TKey key, TValue value) => new()
    {
        Flags = RecordFlags.None,
        Key = key,
        Value = value
    };
}
