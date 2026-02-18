using System;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents an entry in the LSM Tree with a key, value, and tombstone flag.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class LsmEntry<TKey, TValue>
{
    /// <summary>
    /// Gets the key of the entry.
    /// </summary>
    public TKey Key { get; }

    /// <summary>
    /// Gets the value of the entry.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Gets a value indicating whether this entry is a tombstone (deleted).
    /// </summary>
    public bool IsTombstone { get; }

    /// <summary>
    /// Gets the timestamp when the entry was created.
    /// </summary>
    public long Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LsmEntry{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="isTombstone">Whether this is a tombstone entry.</param>
    /// <param name="timestamp">The timestamp.</param>
    public LsmEntry(TKey key, TValue value, bool isTombstone, long timestamp)
    {
        Key = key;
        Value = value;
        IsTombstone = isTombstone;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Represents a Sorted String Table (SSTable) in the LSM Tree.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class SsTable<TKey, TValue> where TKey : IComparable<TKey>
{
    /// <summary>
    /// Gets the sorted entries in this SSTable.
    /// </summary>
    public List<LsmEntry<TKey, TValue>> Entries { get; }

    /// <summary>
    /// Gets the level of this SSTable in the LSM tree hierarchy.
    /// </summary>
    public int Level { get; }

    /// <summary>
    /// Gets the unique identifier of this SSTable.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SsTable{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="entries">The entries to store.</param>
    /// <param name="level">The level in the hierarchy.</param>
    public SsTable(List<LsmEntry<TKey, TValue>> entries, int level)
    {
        Entries = entries.OrderBy(e => e.Key).ToList();
        Level = level;
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Searches for a key in this SSTable.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The entry if found, otherwise null.</returns>
    public LsmEntry<TKey, TValue> Find(TKey key)
    {
        int left = 0, right = Entries.Count - 1;
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int cmp = Entries[mid].Key.CompareTo(key);
            if (cmp == 0)
                return Entries[mid];
            if (cmp < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }
        return null;
    }
}

/// <summary>
/// Represents a node wrapper for ITreeNode interface compatibility.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class LsmTreeNodeWrapper<TKey, TValue> : ITreeNode<KeyValuePair<TKey, TValue>>
{
    private readonly TKey _key;
    private readonly TValue _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="LsmTreeNodeWrapper{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public LsmTreeNodeWrapper(TKey key, TValue value)
    {
        _key = key;
        _value = value;
    }

    /// <inheritdoc />
    public KeyValuePair<TKey, TValue> Value
    {
        get => new(_key, _value);
        set => throw new NotSupportedException("Cannot modify LSM Tree node value directly");
    }

    /// <inheritdoc />
    public IEnumerable<ITreeNode<KeyValuePair<TKey, TValue>>> Children
    {
        get { yield break; }
    }
}

/// <summary>
/// Represents an LSM (Log-Structured Merge) Tree, a write-optimized data structure.
/// </summary>
/// <typeparam name="TKey">The type of the keys, must be comparable.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
public class LsmTree<TKey, TValue> : ITree<KeyValuePair<TKey, TValue>> where TKey : IComparable<TKey>
{
    private readonly SortedDictionary<TKey, LsmEntry<TKey, TValue>> _memTable;
    private readonly List<SsTable<TKey, TValue>> _ssTables;
    private readonly int _memTableThreshold;
    private long _timestamp;
    private readonly object _lock = new();

    /// <summary>
    /// Gets the number of entries in the memtable.
    /// </summary>
    public int MemTableCount => _memTable.Count;

    /// <summary>
    /// Gets the number of SSTables.
    /// </summary>
    public int SsTableCount => _ssTables.Count;

    /// <summary>
    /// Initializes a new instance of the <see cref="LsmTree{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="memTableThreshold">The maximum number of entries before flushing to disk.</param>
    public LsmTree(int memTableThreshold = 1000)
    {
        _memTable = new SortedDictionary<TKey, LsmEntry<TKey, TValue>>();
        _ssTables = new List<SsTable<TKey, TValue>>();
        _memTableThreshold = memTableThreshold;
        _timestamp = 0;
    }

    /// <summary>
    /// Inserts or updates a key-value pair.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    public void Put(TKey key, TValue value)
    {
        lock (_lock)
        {
            var entry = new LsmEntry<TKey, TValue>(key, value, false, ++_timestamp);
            _memTable[key] = entry;

            if (_memTable.Count >= _memTableThreshold)
            {
                FlushMemTable();
            }
        }
    }

    /// <inheritdoc />
    public void Insert(KeyValuePair<TKey, TValue> item)
    {
        Put(item.Key, item.Value);
    }

    /// <summary>
    /// Retrieves a value by key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>The value if found, otherwise default.</returns>
    public TValue Get(TKey key)
    {
        lock (_lock)
        {
            // Check memtable first (most recent)
            if (_memTable.TryGetValue(key, out var memEntry))
            {
                return memEntry.IsTombstone ? default : memEntry.Value;
            }

            // Check SSTables from newest to oldest
            foreach (var ssTable in _ssTables.OrderByDescending(s => s.Level))
            {
                var entry = ssTable.Find(key);
                if (entry != null)
                {
                    return entry.IsTombstone ? default : entry.Value;
                }
            }

            return default;
        }
    }

    /// <inheritdoc />
    public ITreeNode<KeyValuePair<TKey, TValue>> Find(KeyValuePair<TKey, TValue> item)
    {
        var value = Get(item.Key);
        if (value != null)
        {
            return new LsmTreeNodeWrapper<TKey, TValue>(item.Key, value);
        }
        return null;
    }

    /// <inheritdoc />
    public void Delete(KeyValuePair<TKey, TValue> item)
    {
        Delete(item.Key);
    }

    /// <summary>
    /// Marks a key as deleted (tombstone).
    /// </summary>
    /// <param name="key">The key to delete.</param>
    public void Delete(TKey key)
    {
        lock (_lock)
        {
            var entry = new LsmEntry<TKey, TValue>(key, default, true, ++_timestamp);
            _memTable[key] = entry;

            if (_memTable.Count >= _memTableThreshold)
            {
                FlushMemTable();
            }
        }
    }

    /// <summary>
    /// Checks if a key exists in the tree.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists and is not deleted, otherwise false.</returns>
    public bool Contains(TKey key)
    {
        lock (_lock)
        {
            // Check memtable first
            if (_memTable.TryGetValue(key, out var memEntry))
            {
                return !memEntry.IsTombstone;
            }

            // Check SSTables
            foreach (var ssTable in _ssTables.OrderByDescending(s => s.Level))
            {
                var entry = ssTable.Find(key);
                if (entry != null)
                {
                    return !entry.IsTombstone;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Returns all key-value pairs in the tree.
    /// </summary>
    /// <returns>An enumerable of all key-value pairs.</returns>
    public IEnumerable<KeyValuePair<TKey, TValue>> GetAll()
    {
        lock (_lock)
        {
            var result = new Dictionary<TKey, LsmEntry<TKey, TValue>>();

            // Add entries from SSTables (oldest first)
            foreach (var ssTable in _ssTables.OrderBy(s => s.Level))
            {
                foreach (var entry in ssTable.Entries)
                {
                    if (!entry.IsTombstone)
                    {
                        result[entry.Key] = entry;
                    }
                    else
                    {
                        result.Remove(entry.Key);
                    }
                }
            }

            // Overlay memtable entries (most recent)
            foreach (var entry in _memTable.Values)
            {
                if (!entry.IsTombstone)
                {
                    result[entry.Key] = entry;
                }
                else
                {
                    result.Remove(entry.Key);
                }
            }

            return result.Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value.Value));
        }
    }

    /// <summary>
    /// Returns a range of key-value pairs.
    /// </summary>
    /// <param name="startKey">The start key (inclusive).</param>
    /// <param name="endKey">The end key (inclusive).</param>
    /// <returns>An enumerable of key-value pairs in the range.</returns>
    public IEnumerable<KeyValuePair<TKey, TValue>> Range(TKey startKey, TKey endKey)
    {
        return GetAll().Where(kv => kv.Key.CompareTo(startKey) >= 0 && kv.Key.CompareTo(endKey) <= 0);
    }

    /// <summary>
    /// Flushes the memtable to an SSTable.
    /// </summary>
    private void FlushMemTable()
    {
        if (_memTable.Count == 0) return;

        var entries = _memTable.Values.ToList();
        var ssTable = new SsTable<TKey, TValue>(entries, 0);
        _ssTables.Add(ssTable);
        _memTable.Clear();

        // Trigger compaction if needed
        MaybeCompact();
    }

    /// <summary>
    /// Performs compaction when the number of SSTables at level 0 exceeds the threshold.
    /// </summary>
    private void MaybeCompact()
    {
        const int maxLevel0Tables = 4;
        var level0Tables = _ssTables.Where(s => s.Level == 0).ToList();

        if (level0Tables.Count >= maxLevel0Tables)
        {
            Compact(level0Tables, 0);
        }
    }

    /// <summary>
    /// Compacts SSTables from one level to the next.
    /// </summary>
    /// <param name="tablesToCompact">The tables to compact.</param>
    /// <param name="sourceLevel">The source level.</param>
    private void Compact(List<SsTable<TKey, TValue>> tablesToCompact, int sourceLevel)
    {
        // Merge all entries from tables to compact
        var mergedEntries = new Dictionary<TKey, LsmEntry<TKey, TValue>>();

        foreach (var table in tablesToCompact.OrderBy(t => t.Level))
        {
            foreach (var entry in table.Entries)
            {
                // Keep the most recent entry for each key
                if (!mergedEntries.ContainsKey(entry.Key) || mergedEntries[entry.Key].Timestamp < entry.Timestamp)
                {
                    mergedEntries[entry.Key] = entry;
                }
            }
        }

        // Remove tombstones and create new SSTable
        var compactedEntries = mergedEntries.Values.Where(e => !e.IsTombstone).ToList();
        if (compactedEntries.Count > 0)
        {
            var newSsTable = new SsTable<TKey, TValue>(compactedEntries, sourceLevel + 1);
            _ssTables.RemoveAll(t => tablesToCompact.Contains(t));
            _ssTables.Add(newSsTable);
        }
        else
        {
            _ssTables.RemoveAll(t => tablesToCompact.Contains(t));
        }
    }

    /// <summary>
    /// Clears all data from the tree.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _memTable.Clear();
            _ssTables.Clear();
            _timestamp = 0;
        }
    }
}
