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
    Internal = 0,
    
    /// <summary>Leaf node containing actual key-value pairs.</summary>
    Leaf = 1
}

/// <summary>
/// Header common to all B+ tree nodes. Stored at the beginning of each page.
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

/// <summary>
/// Represents an internal node in a B+ tree stored directly in a memory page.
/// Internal nodes store keys and child page references.
/// </summary>
/// <typeparam name="TKey">The type of keys, must be unmanaged and comparable.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public struct BPlusInternalNode<TKey> where TKey : unmanaged, IComparable<TKey>
{
    /// <summary>Node header containing metadata.</summary>
    public BPlusNodeHeader Header;
    
    /// <summary>Inline array of keys. Actual count stored in Header.KeyCount.</summary>
    private TKey _key0;
    
    /// <summary>Inline array of child page IDs. One more than key count.</summary>
    private long _childPageId0;

    /// <summary>Gets the span of active keys in this node.</summary>
    public Span<TKey> GetKeys(int maxDegree)
    {
        ref var start = ref Unsafe.AsRef(in _key0);
        return MemoryMarshal.CreateSpan(ref start, Math.Min(Header.KeyCount, maxDegree - 1));
    }

    /// <summary>Gets the span of child page IDs in this node.</summary>
    public Span<long> GetChildPageIds(int maxDegree)
    {
        ref var start = ref Unsafe.AsRef(in _childPageId0);
        return MemoryMarshal.CreateSpan(ref start, Math.Min(Header.KeyCount + 1, maxDegree));
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
        // Account for header
        var availableSpace = pageSize - Unsafe.SizeOf<BPlusNodeHeader>();
        // Each key takes sizeof(TKey), each child takes sizeof(long)
        // We fit n keys and n+1 children: n*sizeof(TKey) + (n+1)*sizeof(long) <= available
        // n * (sizeof(TKey) + sizeof(long)) + sizeof(long) <= available
        // n <= (available - sizeof(long)) / (sizeof(TKey) + sizeof(long))
        var keyPlusChildSize = Unsafe.SizeOf<TKey>() + sizeof(long);
        var maxN = (availableSpace - sizeof(long)) / keyPlusChildSize;
        return Math.Max(2, (int)maxN); // Minimum degree of 2
    }
}

/// <summary>
/// Represents a leaf node in a B+ tree stored directly in a memory page.
/// Leaf nodes store actual key-value pairs.
/// </summary>
/// <typeparam name="TKey">The type of keys, must be unmanaged and comparable.</typeparam>
/// <typeparam name="TValue">The type of values, must be unmanaged.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public struct BPlusLeafNode<TKey, TValue> 
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    /// <summary>Node header containing metadata.</summary>
    public BPlusNodeHeader Header;
    
    /// <summary>Inline array of keys. Actual count stored in Header.KeyCount.</summary>
    private TKey _key0;
    
    /// <summary>Inline array of values parallel to keys.</summary>
    private TValue _value0;

    /// <summary>Gets the span of active keys in this node.</summary>
    public Span<TKey> GetKeys(int maxDegree)
    {
        ref var start = ref Unsafe.AsRef(in _key0);
        return MemoryMarshal.CreateSpan(ref start, Math.Min(Header.KeyCount, maxDegree - 1));
    }

    /// <summary>Gets the span of values in this node.</summary>
    public Span<TValue> GetValues(int maxDegree)
    {
        ref var start = ref Unsafe.AsRef(in _value0);
        return MemoryMarshal.CreateSpan(ref start, Math.Min(Header.KeyCount, maxDegree - 1));
    }

    /// <summary>Gets or sets a key at the specified index.</summary>
    public ref TKey GetKey(int index, int maxDegree)
    {
        if ((uint)index >= (uint)(maxDegree - 1))
            throw new ArgumentOutOfRangeException(nameof(index));
        
        ref var start = ref Unsafe.AsRef(in _key0);
        return ref Unsafe.Add(ref start, index);
    }

    /// <summary>Gets or sets a value at the specified index.</summary>
    public ref TValue GetValue(int index, int maxDegree)
    {
        if ((uint)index >= (uint)(maxDegree - 1))
            throw new ArgumentOutOfRangeException(nameof(index));
        
        ref var start = ref Unsafe.AsRef(in _value0);
        return ref Unsafe.Add(ref start, index);
    }

    /// <summary>Gets the page ID of the next leaf node for range scans.</summary>
    public long NextPageId => Header.NextSiblingPageId;

    /// <summary>Calculates the maximum degree based on page size, key size, and value size.</summary>
    public static int CalculateDegree(int pageSize)
    {
        // Account for header
        var availableSpace = pageSize - Unsafe.SizeOf<BPlusNodeHeader>();
        // We fit n key-value pairs: n * (sizeof(TKey) + sizeof(TValue)) <= available
        var entrySize = Unsafe.SizeOf<TKey>() + Unsafe.SizeOf<TValue>();
        var maxN = availableSpace / entrySize;
        return Math.Max(2, (int)maxN); // Minimum degree of 2
    }
}
