using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aero.DataStructures.Trees.Persistence.Nodes;

/// <summary>
/// Identifies the type of node in a B+ tree.
/// </summary>
public enum NodeType : byte
{
    /// <summary>Internal node containing keys and child page references.</summary>
    Internal = 0x01,
    
    /// <summary>Leaf node containing actual key-value pairs.</summary>
    Leaf = 0x02
}

/// <summary>
/// Represents an internal node in a B+ tree stored directly in a memory page.
/// Internal nodes store keys and child page references.
/// </summary>
/// <typeparam name="TKey">The type of keys, must be unmanaged and comparable.</typeparam>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BPlusInternalNode<TKey> where TKey : unmanaged, IComparable<TKey>
{
    /// <summary>Node type identifier. Always 0x01 for internal nodes.</summary>
    public NodeType NodeType;
    
    /// <summary>Number of keys currently stored in this node.</summary>
    public int KeyCount;
    
    /// <summary>
    /// Inline array of child page IDs. One more than key count.
    /// Access via GetChildPageIds() method.
    /// </summary>
    private long _childPageId0;
    
    /// <summary>
    /// Inline array of keys. Actual count stored in KeyCount.
    /// Access via GetKeys() method.
    /// </summary>
    private TKey _key0;

    /// <summary>Gets the span of active keys in this node.</summary>
    public Span<TKey> GetKeys(int maxDegree)
    {
        ref var start = ref Unsafe.AsRef(in _key0);
        return MemoryMarshal.CreateSpan(ref start, Math.Min(KeyCount, maxDegree - 1));
    }

    /// <summary>Gets the span of child page IDs in this node.</summary>
    public Span<long> GetChildPageIds(int maxDegree)
    {
        ref var start = ref Unsafe.AsRef(in _childPageId0);
        return MemoryMarshal.CreateSpan(ref start, Math.Min(KeyCount + 1, maxDegree));
    }

    /// <summary>Gets or sets a key at the specified index.</summary>
    public ref TKey GetKey(int index, int maxDegree)
    {
        if ((uint)index >= (uint)(maxDegree - 1))
            throw new ArgumentOutOfRangeException(nameof(index));
        
        ref var start = ref Unsafe.AsRef(in _key0);
        return ref Unsafe.Add(ref start, index);
    }

    /// <summary>Gets or sets a child page ID at the specified index.</summary>
    public ref long GetChildPageId(int index, int maxDegree)
    {
        if ((uint)index >= (uint)maxDegree)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        ref var start = ref Unsafe.AsRef(in _childPageId0);
        return ref Unsafe.Add(ref start, index);
    }

    /// <summary>Calculates the maximum degree based on page size and key size.</summary>
    public static int CalculateDegree(int pageSize)
    {
        // NodeType(1) + KeyCount(4) + padding(3) = 8 bytes header
        var headerSize = 8;
        var availableSpace = pageSize - headerSize;
        // Each key takes sizeof(TKey), each child takes sizeof(long)
        // We fit n keys and n+1 children: n*sizeof(TKey) + (n+1)*sizeof(long) <= available
        var keyPlusChildSize = Unsafe.SizeOf<TKey>() + sizeof(long);
        var maxN = (availableSpace - sizeof(long)) / keyPlusChildSize;
        return Math.Max(2, (int)maxN); // Minimum degree of 2
    }
}

/// <summary>
/// Represents a leaf node in a B+ tree stored directly in a memory page.
/// Leaf nodes store actual key-value pairs with support for tombstone deletion.
/// </summary>
/// <typeparam name="TKey">The type of keys, must be unmanaged and comparable.</typeparam>
/// <typeparam name="TValue">The type of values, must be unmanaged.</typeparam>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BPlusLeafNode<TKey, TValue> 
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    /// <summary>Node type identifier. Always 0x02 for leaf nodes.</summary>
    public NodeType NodeType;
    
    /// <summary>Physical capacity of the record buffer (total slots available).</summary>
    public int TotalSlots;
    
    /// <summary>Number of non-tombstoned (live) records.</summary>
    public int LiveCount;
    
    /// <summary>Number of tombstoned (deleted) records.</summary>
    public int DeadCount;
    
    /// <summary>Page ID of the previous leaf node in the linked list. -1 if first leaf.</summary>
    public long PrevLeafPageId;
    
    /// <summary>Page ID of the next leaf node in the linked list. -1 if last leaf.</summary>
    public long NextLeafPageId;
    
    /// <summary>
    /// Inline array of records. Access via GetRecords() method.
    /// </summary>
    private BPlusLeafRecord<TKey, TValue> _record0;

    /// <summary>Gets the fragmentation ratio of this page (dead slots / total slots).</summary>
    public double Fragmentation => TotalSlots == 0 ? 0.0 : (double)DeadCount / TotalSlots;

    /// <summary>
    /// Returns true if this page needs compaction at the given threshold.
    /// </summary>
    /// <param name="threshold">Fragmentation threshold (default 0.5 = 50% deleted).</param>
    public bool NeedsCompaction(double threshold = 0.5) => Fragmentation >= threshold;

    /// <summary>
    /// Gets the span of all record slots in this node.
    /// </summary>
    /// <param name="capacity">The maximum capacity of record slots in this page.</param>
    public Span<BPlusLeafRecord<TKey, TValue>> GetRecords(int capacity)
    {
        ref var start = ref Unsafe.AsRef(in _record0);
        return MemoryMarshal.CreateSpan(ref start, capacity);
    }

    /// <summary>
    /// Gets a reference to a record at the specified index.
    /// </summary>
    public ref BPlusLeafRecord<TKey, TValue> GetRecord(int index, int capacity)
    {
        if ((uint)index >= (uint)capacity)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        ref var start = ref Unsafe.AsRef(in _record0);
        return ref Unsafe.Add(ref start, index);
    }

    /// <summary>
    /// Counts the actual live records by scanning.
    /// Updates LiveCount and DeadCount fields.
    /// </summary>
    public void RecountLiveRecords(int capacity)
    {
        var records = GetRecords(capacity);
        LiveCount = 0;
        DeadCount = 0;
        
        for (int i = 0; i < TotalSlots && i < capacity; i++)
        {
            if (records[i].IsLive)
                LiveCount++;
            else if (records[i].IsDeleted)
                DeadCount++;
        }
    }

    /// <summary>
    /// Calculates the maximum capacity based on page size, key size, and value size.
    /// </summary>
    public static int CalculateCapacity(int pageSize)
    {
        // NodeType(1) + TotalSlots(4) + LiveCount(4) + DeadCount(4) + 
        // PrevLeafPageId(8) + NextLeafPageId(8) = 29 bytes, pad to 32
        var headerSize = 32;
        var availableSpace = pageSize - headerSize;
        var recordSize = Unsafe.SizeOf<BPlusLeafRecord<TKey, TValue>>();
        var maxRecords = availableSpace / recordSize;
        return Math.Max(2, maxRecords); // Minimum capacity of 2
    }
}

/// <summary>
/// Legacy header structure for backward compatibility.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BPlusNodeHeader
{
    /// <summary>The type of this node (internal or leaf).</summary>
    public NodeType NodeType;
    
    /// <summary>Number of keys currently stored in this node.</summary>
    public ushort KeyCount;
    
    /// <summary>Page ID of the next sibling node (for leaf nodes in range scans).</summary>
    public long NextSiblingPageId;
    
    /// <summary>Reserved for alignment and future use.</summary>
    public ulong Reserved;
}
