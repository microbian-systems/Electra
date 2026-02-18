namespace Aero.DataStructures.Trees.Persistence.Nodes;

/// <summary>
/// Flags for individual B+ tree leaf records.
/// Used to track tombstones and overflow status.
/// </summary>
[Flags]
public enum RecordFlags : byte
{
    /// <summary>
    /// No flags set — record is live and normal.
    /// </summary>
    None = 0x00,
    
    /// <summary>
    /// Record has been logically deleted (tombstone).
    /// The key is retained but the value is cleared.
    /// </summary>
    Deleted = 0x01,
    
    /// <summary>
    /// Value is stored in an overflow page (reserved for future use).
    /// </summary>
    Overflow = 0x02
}
