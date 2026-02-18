using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Interfaces;
using Aero.DataStructures.Trees.Persistence.Nodes;
using Aero.DataStructures.Trees.Persistence.Readers;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Trees;

/// <summary>
/// A persistent B+ tree implementation with tombstone deletion and vacuum support.
/// Uses IStorageBackend for storage and automatically selects zero-copy path when available.
/// </summary>
/// <typeparam name="TKey">The type of keys, must be unmanaged and comparable.</typeparam>
/// <typeparam name="TValue">The type of values, must be unmanaged.</typeparam>
public sealed class PersistentBPlusTree<TKey, TValue> : Interfaces.ITree<TKey>, IOrderedTree<TKey>, IVacuumable, IAsyncDisposable
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    private const double DefaultCompactionThreshold = 0.5;
    private const long NullPageId = -1;

    private readonly IStorageBackend _storage;
    private readonly NodeReader<TKey, TValue> _reader;
    private long _rootPageId;
    private bool _disposed;

    /// <summary>Gets whether this tree is using zero-copy access.</summary>
    public bool IsZeroCopy => _storage is IZeroCopyStorageBackend;

    /// <summary>Gets the storage backend used by this tree.</summary>
    public IStorageBackend Storage => _storage;

    /// <summary>Gets the leaf capacity for this tree.</summary>
    public int LeafCapacity => _reader.LeafCapacity;

    /// <summary>Gets the internal node degree for this tree.</summary>
    public int InternalDegree => _reader.InternalDegree;

    /// <summary>
    /// Creates a new persistent B+ tree using the specified storage backend.
    /// </summary>
    public PersistentBPlusTree(IStorageBackend storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _reader = NodeReader<TKey, TValue>.Create(storage);
        
        if (storage.PageCount == 0)
        {
            _rootPageId = InitializeNewTreeAsync().GetAwaiter().GetResult();
        }
        else
        {
            _rootPageId = 1;
        }
    }

    private async Task<long> InitializeNewTreeAsync()
    {
        var rootPageId = await _storage.AllocatePageAsync();
        
        var rootNode = new BPlusLeafNode<TKey, TValue>
        {
            NodeType = NodeType.Leaf,
            TotalSlots = 0,
            LiveCount = 0,
            DeadCount = 0,
            PrevLeafPageId = NullPageId,
            NextLeafPageId = NullPageId
        };
        
        await _reader.WriteLeafAsync(rootPageId, rootNode, CancellationToken.None);
        await _storage.FlushAsync();
        
        return rootPageId;
    }

    #region Core Operations

    /// <summary>
    /// Inserts a key-value pair into the tree.
    /// </summary>
    public async ValueTask InsertAsync(TKey key, TValue value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var leafPageId = await FindLeafPageAsync(key, ct);
        var leaf = await _reader.ReadLeafAsync(leafPageId, ct);
        var records = leaf.GetRecords(_reader.LeafCapacity);
        
        // Find insert position
        var insertIndex = FindInsertIndex(records, leaf.TotalSlots, key);
        
        // Check if key exists (update)
        for (int i = 0; i < leaf.TotalSlots; i++)
        {
            if (!records[i].IsDeleted && records[i].Key.Equals(key))
            {
                records[i] = BPlusLeafRecord<TKey, TValue>.Live(key, value);
                await _reader.WriteLeafAsync(leafPageId, leaf, ct);
                return;
            }
        }
        
        // Insert new record
        if (leaf.TotalSlots >= _reader.LeafCapacity)
        {
            // Need to split - simplified for now
            await SplitAndInsertAsync(leafPageId, leaf, key, value, ct);
            return;
        }
        
        // Shift records to make room
        for (int i = leaf.TotalSlots; i > insertIndex; i--)
        {
            records[i] = records[i - 1];
        }
        
        records[insertIndex] = BPlusLeafRecord<TKey, TValue>.Live(key, value);
        leaf.TotalSlots++;
        leaf.LiveCount++;
        
        await _reader.WriteLeafAsync(leafPageId, leaf, ct);
        await _storage.UpdatePageMetadataAsync(leafPageId, liveDelta: 1, deadDelta: 0, ct);
    }

    /// <summary>
    /// ITree&lt;TKey&gt; explicit implementation. 
    /// Use InsertAsync(TKey key, TValue value) for B+ trees.
    /// </summary>
    ValueTask Interfaces.ITree<TKey>.InsertAsync(TKey key, CancellationToken ct)
    {
        throw new NotSupportedException(
            "B+ tree requires both key and value. Use InsertAsync(TKey key, TValue value) instead.");
    }

    public async ValueTask<bool> DeleteAsync(TKey key, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var leafPageId = await FindLeafPageAsync(key, ct);
        var leaf = await _reader.ReadLeafAsync(leafPageId, ct);
        var records = leaf.GetRecords(_reader.LeafCapacity);
        
        // Find the record
        for (int i = 0; i < leaf.TotalSlots; i++)
        {
            if (!records[i].IsDeleted && records[i].Key.Equals(key))
            {
                // Tombstone the record
                records[i].MarkDeleted();
                leaf.LiveCount--;
                leaf.DeadCount++;
                
                await _reader.WriteLeafAsync(leafPageId, leaf, ct);
                await _storage.UpdatePageMetadataAsync(leafPageId, liveDelta: -1, deadDelta: 1, ct);
                
                // Check if compaction needed
                if (leaf.NeedsCompaction(DefaultCompactionThreshold))
                {
                    await CompactPageAsync(leafPageId, ct);
                }
                
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Checks if the tree contains the specified key.
    /// </summary>
    public async ValueTask<bool> ContainsAsync(TKey key, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var (found, _) = await FindRecordAsync(key, ct);
        return found;
    }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    public async ValueTask<(bool found, TValue value)> TryGetAsync(TKey key, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var (found, record) = await FindRecordAsync(key, ct);
        return found ? (true, record.Value) : (false, default);
    }

    #endregion

    #region IVacuumable

    public async ValueTask<double> GetFragmentationAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        long totalSlots = 0;
        long deadSlots = 0;
        
        await foreach (var meta in _storage.GetFragmentedPagesAsync(0.0, ct))
        {
            totalSlots += meta.TotalSlots;
            deadSlots += meta.DeadSlots;
        }
        
        return totalSlots == 0 ? 0.0 : (double)deadSlots / totalSlots;
    }

    public async ValueTask<bool> VacuumPageAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        await foreach (var meta in _storage.GetFragmentedPagesAsync(DefaultCompactionThreshold, ct))
        {
            await CompactPageAsync(meta.PageId, ct);
            return true;
        }
        
        return false;
    }

    public async ValueTask VacuumAsync(
        double fragmentationThreshold = DefaultCompactionThreshold,
        IProgress<VacuumProgress>? progress = null,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var pagesToCompact = new List<PageMetadata>();
        await foreach (var meta in _storage.GetFragmentedPagesAsync(fragmentationThreshold, ct))
        {
            pagesToCompact.Add(meta);
        }
        
        int processed = 0;
        int compacted = 0;
        long bytesReclaimed = 0;
        int recordSize = System.Runtime.InteropServices.Marshal.SizeOf<BPlusLeafRecord<TKey, TValue>>();
        
        foreach (var meta in pagesToCompact)
        {
            ct.ThrowIfCancellationRequested();
            
            var beforeDead = meta.DeadSlots;
            await CompactPageAsync(meta.PageId, ct);
            
            compacted++;
            bytesReclaimed += beforeDead * recordSize;
            
            processed++;
            progress?.Report(new VacuumProgress(
                pagesToCompact.Count, processed, compacted, bytesReclaimed));
        }
    }

    private async ValueTask CompactPageAsync(long pageId, CancellationToken ct)
    {
        var leaf = await _reader.ReadLeafAsync(pageId, ct);
        var records = leaf.GetRecords(_reader.LeafCapacity);
        
        // Two-pointer pack: move live records to front
        int writeIndex = 0;
        for (int readIndex = 0; readIndex < leaf.TotalSlots; readIndex++)
        {
            if (!records[readIndex].IsDeleted)
            {
                if (writeIndex != readIndex)
                {
                    records[writeIndex] = records[readIndex];
                }
                writeIndex++;
            }
        }
        
        // Zero out the tail
        for (int i = writeIndex; i < leaf.TotalSlots; i++)
        {
            records[i] = default;
        }
        
        int reclaimed = leaf.DeadCount;
        leaf.TotalSlots = writeIndex;
        leaf.DeadCount = 0;
        leaf.LiveCount = writeIndex;
        
        await _reader.WriteLeafAsync(pageId, leaf, ct);
        await _storage.UpdatePageMetadataAsync(pageId, liveDelta: 0, deadDelta: -reclaimed, ct);
    }

    #endregion

    #region IOrderedTree<TKey>

    public async ValueTask<TKey> MinAsync(CancellationToken ct = default)
    {
        var (found, key) = await TryGetMinAsync(ct);
        if (!found)
            throw new InvalidOperationException("Tree is empty.");
        return key;
    }

    public async ValueTask<TKey> MaxAsync(CancellationToken ct = default)
    {
        var (found, key) = await TryGetMaxAsync(ct);
        if (!found)
            throw new InvalidOperationException("Tree is empty.");
        return key;
    }

    public async ValueTask<(bool found, TKey value)> TryGetMinAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var pageId = _rootPageId;
        
        while (true)
        {
            var node = await _reader.ReadInternalAsync(pageId, ct);
            
            if (node.NodeType == NodeType.Leaf)
            {
                var leaf = await _reader.ReadLeafAsync(pageId, ct);
                var records = leaf.GetRecords(_reader.LeafCapacity);
                
                for (int i = 0; i < leaf.TotalSlots; i++)
                {
                    if (records[i].IsLive)
                        return (true, records[i].Key);
                }
                return (false, default);
            }
            
            pageId = node.GetChildPageIds(_reader.InternalDegree)[0];
        }
    }

    public async ValueTask<(bool found, TKey value)> TryGetMaxAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var pageId = _rootPageId;
        
        while (true)
        {
            var node = await _reader.ReadInternalAsync(pageId, ct);
            
            if (node.NodeType == NodeType.Leaf)
            {
                var leaf = await _reader.ReadLeafAsync(pageId, ct);
                var records = leaf.GetRecords(_reader.LeafCapacity);
                
                for (int i = leaf.TotalSlots - 1; i >= 0; i--)
                {
                    if (records[i].IsLive)
                        return (true, records[i].Key);
                }
                return (false, default);
            }
            
            pageId = node.GetChildPageIds(_reader.InternalDegree)[node.KeyCount];
        }
    }

    public async IAsyncEnumerable<TKey> InOrderAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        // Navigate to leftmost leaf
        var pageId = _rootPageId;
        while (true)
        {
            var node = await _reader.ReadInternalAsync(pageId, ct);
            if (node.NodeType == NodeType.Leaf) break;
            pageId = node.GetChildPageIds(_reader.InternalDegree)[0];
        }
        
        // Scan all leaves
        while (pageId != NullPageId)
        {
            ct.ThrowIfCancellationRequested();
            
            var leaf = await _reader.ReadLeafAsync(pageId, ct);
            var records = leaf.GetRecords(_reader.LeafCapacity);
            
            // Copy keys to avoid span across await boundary
            var keysToYield = new List<TKey>(leaf.TotalSlots);
            for (int i = 0; i < leaf.TotalSlots; i++)
            {
                if (records[i].IsLive)
                    keysToYield.Add(records[i].Key);
            }
            
            foreach (var key in keysToYield)
                yield return key;
            
            pageId = leaf.NextLeafPageId;
        }
    }

    public async IAsyncEnumerable<TKey> ScanAsync(TKey from, TKey to, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var pageId = await FindLeafPageAsync(from, ct);
        
        while (pageId != NullPageId)
        {
            ct.ThrowIfCancellationRequested();
            
            var leaf = await _reader.ReadLeafAsync(pageId, ct);
            var records = leaf.GetRecords(_reader.LeafCapacity);
            
            // Copy matching keys to avoid span across await boundary
            var keysToYield = new List<TKey>();
            bool pastEnd = false;
            
            for (int i = 0; i < leaf.TotalSlots; i++)
            {
                if (!records[i].IsLive)
                    continue;
                    
                var key = records[i].Key;
                
                if (key.CompareTo(from) < 0)
                    continue;
                    
                if (key.CompareTo(to) > 0)
                {
                    pastEnd = true;
                    break;
                }
                
                keysToYield.Add(key);
            }
            
            foreach (var key in keysToYield)
                yield return key;
            
            if (pastEnd)
                break;
            
            pageId = leaf.NextLeafPageId;
        }
    }

    public ValueTask<long> CountAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        // TODO: Track count in metadata
        return new ValueTask<long>(0L);
    }

    public ValueTask ClearAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        throw new NotImplementedException();
    }

    #endregion

    #region Private Helpers

    private async ValueTask<(bool found, BPlusLeafRecord<TKey, TValue> record)> FindRecordAsync(TKey key, CancellationToken ct)
    {
        var leafPageId = await FindLeafPageAsync(key, ct);
        var leaf = await _reader.ReadLeafAsync(leafPageId, ct);
        var records = leaf.GetRecords(_reader.LeafCapacity);
        
        for (int i = 0; i < leaf.TotalSlots; i++)
        {
            if (!records[i].IsDeleted && records[i].Key.Equals(key))
                return (true, records[i]);
        }
        
        return (false, default);
    }

    private async ValueTask<long> FindLeafPageAsync(TKey key, CancellationToken ct)
    {
        var pageId = _rootPageId;
        
        while (true)
        {
            var node = await _reader.ReadInternalAsync(pageId, ct);
            
            if (node.NodeType == NodeType.Leaf)
                return pageId;
            
            var keys = node.GetKeys(_reader.InternalDegree);
            var children = node.GetChildPageIds(_reader.InternalDegree);
            
            int i = 0;
            while (i < node.KeyCount && key.CompareTo(keys[i]) >= 0)
                i++;
            
            pageId = children[i];
        }
    }

    private int FindInsertIndex(Span<BPlusLeafRecord<TKey, TValue>> records, int count, TKey key)
    {
        int lo = 0, hi = count;
        
        while (lo < hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            if (records[mid].Key.CompareTo(key) < 0)
                lo = mid + 1;
            else
                hi = mid;
        }
        
        return lo;
    }

    private async ValueTask SplitAndInsertAsync(
        long leafPageId, 
        BPlusLeafNode<TKey, TValue> leaf, 
        TKey key, 
        TValue value, 
        CancellationToken ct)
    {
        var newLeafPageId = await _storage.AllocatePageAsync(ct);
        
        var newLeaf = new BPlusLeafNode<TKey, TValue>
        {
            NodeType = NodeType.Leaf,
            TotalSlots = 0,
            LiveCount = 0,
            DeadCount = 0,
            PrevLeafPageId = leafPageId,
            NextLeafPageId = leaf.NextLeafPageId
        };
        
        var oldRecords = leaf.GetRecords(_reader.LeafCapacity);
        var newRecords = newLeaf.GetRecords(_reader.LeafCapacity);
        
        int mid = leaf.TotalSlots / 2;
        
        // Move upper half to new leaf
        for (int i = mid; i < leaf.TotalSlots; i++)
        {
            newRecords[i - mid] = oldRecords[i];
            if (newRecords[i - mid].IsLive)
                newLeaf.LiveCount++;
            else
                newLeaf.DeadCount++;
            newLeaf.TotalSlots++;
        }
        
        leaf.TotalSlots = mid;
        leaf.LiveCount = 0;
        leaf.DeadCount = 0;
        for (int i = 0; i < leaf.TotalSlots; i++)
        {
            if (oldRecords[i].IsLive)
                leaf.LiveCount++;
            else
                leaf.DeadCount++;
        }
        
        leaf.NextLeafPageId = newLeafPageId;
        
        // Save separator key before any await (span cannot cross await boundary)
        var separatorKey = newRecords[0].Key;
        
        // Insert into appropriate leaf
        if (key.CompareTo(separatorKey) < 0)
        {
            // Insert into old leaf
            var insertIndex = FindInsertIndex(oldRecords, leaf.TotalSlots, key);
            for (int i = leaf.TotalSlots; i > insertIndex; i--)
                oldRecords[i] = oldRecords[i - 1];
            oldRecords[insertIndex] = BPlusLeafRecord<TKey, TValue>.Live(key, value);
            leaf.TotalSlots++;
            leaf.LiveCount++;
        }
        else
        {
            // Insert into new leaf
            var insertIndex = FindInsertIndex(newRecords, newLeaf.TotalSlots, key);
            for (int i = newLeaf.TotalSlots; i > insertIndex; i--)
                newRecords[i] = newRecords[i - 1];
            newRecords[insertIndex] = BPlusLeafRecord<TKey, TValue>.Live(key, value);
            newLeaf.TotalSlots++;
            newLeaf.LiveCount++;
        }
        
        await _reader.WriteLeafAsync(leafPageId, leaf, ct);
        await _reader.WriteLeafAsync(newLeafPageId, newLeaf, ct);
        
        await _storage.UpdatePageMetadataAsync(leafPageId, 0, 0, ct);
        await _storage.UpdatePageMetadataAsync(newLeafPageId, newLeaf.LiveCount, 0, ct);
        
        // Create new root if needed (simplified)
        if (leafPageId == _rootPageId)
        {
            var newRootPageId = await _storage.AllocatePageAsync(ct);
            
            var newRoot = new BPlusInternalNode<TKey>
            {
                NodeType = NodeType.Internal,
                KeyCount = 1
            };
            
            newRoot.GetKeys(_reader.InternalDegree)[0] = separatorKey;
            newRoot.GetChildPageIds(_reader.InternalDegree)[0] = leafPageId;
            newRoot.GetChildPageIds(_reader.InternalDegree)[1] = newLeafPageId;
            
            await _reader.WriteInternalAsync(newRootPageId, newRoot, ct);
            _rootPageId = newRootPageId;
        }
    }

    #endregion

    public async ValueTask FlushAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _storage.FlushAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await FlushAsync();
            await _storage.DisposeAsync();
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PersistentBPlusTree<TKey, TValue>));
    }
}
