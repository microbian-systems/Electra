using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Nodes;
using Aero.DataStructures.Trees.Persistence.Readers;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Trees;

/// <summary>
/// A persistent B+ tree implementation that uses IStorageBackend for storage.
/// Automatically selects zero-copy access when the backend supports IZeroCopyStorageBackend.
/// </summary>
/// <remarks>
/// This design composes both abstraction and performance:
/// - Uses IStorageBackend as the contract (any backend can be used)
/// - Allows zero-copy access as an optional capability via IZeroCopyStorageBackend
/// - NodeReader abstraction chosen at construction routes to optimal implementation
/// </remarks>
/// <typeparam name="TKey">The type of keys, must be unmanaged and comparable.</typeparam>
/// <typeparam name="TValue">The type of values, must be unmanaged.</typeparam>
public sealed class PersistentBPlusTree<TKey, TValue> : IAsyncDisposable
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    private readonly IStorageBackend _storage;
    private readonly NodeReader<TKey, TValue> _reader;
    private readonly int _maxDegree;
    private long _rootPageId;
    private bool _disposed;

    /// <summary>Gets the maximum degree (order) of the B+ tree.</summary>
    public int Degree => _maxDegree;

    /// <summary>Gets the storage backend used by this tree.</summary>
    public IStorageBackend Storage => _storage;

    /// <summary>Gets whether this tree is using zero-copy access (fast path).</summary>
    public bool IsZeroCopy => _storage is IZeroCopyStorageBackend;

    /// <summary>
    /// Creates a new persistent B+ tree using the specified storage backend.
    /// </summary>
    /// <param name="storage">The storage backend to use for persistence.</param>
    public PersistentBPlusTree(IStorageBackend storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _reader = NodeReader<TKey, TValue>.Create(storage);
        _maxDegree = CalculateMaxDegree(storage.PageSize);
        
        // Initialize root - check if tree already exists
        if (storage.PageCount == 0)
        {
            // New tree - create empty root leaf node
            _rootPageId = InitializeNewTree().GetAwaiter().GetResult();
        }
        else
        {
            // Existing tree - root is at page 1 (page 0 is header)
            _rootPageId = 1;
        }
    }

    private async Task<long> InitializeNewTree()
    {
        // Allocate root page
        var rootPageId = await _storage.AllocatePageAsync();
        
        // Initialize as empty leaf
        var rootNode = new BPlusLeafNode<TKey, TValue>
        {
            Header = new BPlusNodeHeader
            {
                NodeType = NodeType.Leaf,
                KeyCount = 0,
                NextSiblingPageId = -1
            }
        };
        
        await _reader.WriteLeafAsync(rootPageId, rootNode, default);
        await _storage.FlushAsync();
        
        return rootPageId;
    }

    private static int CalculateMaxDegree(int pageSize)
    {
        var internalDegree = BPlusInternalNode<TKey>.CalculateDegree(pageSize);
        var leafDegree = BPlusLeafNode<TKey, TValue>.CalculateDegree(pageSize);
        return Math.Min(internalDegree, leafDegree);
    }

    #region Core Operations

    /// <summary>
    /// Checks if the tree contains the specified key.
    /// </summary>
    public async ValueTask<bool> ContainsAsync(TKey key, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var leafPageId = await FindLeafPageAsync(key, ct);
        var leaf = await _reader.ReadLeafAsync(leafPageId, ct);
        
        return BinarySearch(leaf.GetKeys(_maxDegree), key) >= 0;
    }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    public async ValueTask<(bool found, TValue value)> TryGetAsync(TKey key, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var leafPageId = await FindLeafPageAsync(key, ct);
        var leaf = await _reader.ReadLeafAsync(leafPageId, ct);
        
        var keys = leaf.GetKeys(_maxDegree);
        var index = BinarySearch(keys, key);
        
        if (index >= 0)
        {
            return (true, leaf.GetValues(_maxDegree)[index]);
        }
        
        return (false, default);
    }

    /// <summary>
    /// Inserts a key-value pair into the tree.
    /// If the key already exists, updates the value.
    /// </summary>
    public async ValueTask InsertAsync(TKey key, TValue value, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        // Navigate to leaf
        var leafPageId = await FindLeafPageAsync(key, ct);
        var leaf = await _reader.ReadLeafAsync(leafPageId, ct);
        
        var keys = leaf.GetKeys(_maxDegree);
        var index = BinarySearch(keys, key);
        
        if (index >= 0)
        {
            // Key exists - update value
            leaf.GetValues(_maxDegree)[index] = value;
            await _reader.WriteLeafAsync(leafPageId, leaf, ct);
            return;
        }
        
        // Insert new key at the correct position
        var insertIndex = ~index;
        InsertIntoLeaf(ref leaf, insertIndex, key, value);
        
        if (leaf.Header.KeyCount < _maxDegree - 1)
        {
            // Leaf has room - just write it back
            await _reader.WriteLeafAsync(leafPageId, leaf, ct);
        }
        else
        {
            // Leaf is full - need to split
            await SplitLeafAsync(leafPageId, leaf, ct);
        }
    }

    /// <summary>
    /// Removes the specified key from the tree.
    /// Returns true if the key was found and removed, false otherwise.
    /// </summary>
    public async ValueTask<bool> DeleteAsync(TKey key, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var leafPageId = await FindLeafPageAsync(key, ct);
        var leaf = await _reader.ReadLeafAsync(leafPageId, ct);
        
        var keys = leaf.GetKeys(_maxDegree);
        var index = BinarySearch(keys, key);
        
        if (index < 0)
            return false;
        
        // Remove key-value pair
        var values = leaf.GetValues(_maxDegree);
        
        for (int i = index; i < leaf.Header.KeyCount - 1; i++)
        {
            keys[i] = keys[i + 1];
            values[i] = values[i + 1];
        }
        
        leaf.Header.KeyCount--;
        await _reader.WriteLeafAsync(leafPageId, leaf, ct);
        
        // Note: Coalescing and redistribution not implemented in this simplified version
        return true;
    }

    #endregion

    #region Range Queries

    /// <summary>
    /// Gets the minimum key in the tree.
    /// </summary>
    public async ValueTask<(bool found, TKey key, TValue value)> MinAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var pageId = _rootPageId;
        
        // Navigate to leftmost leaf
        while (true)
        {
            var node = await _reader.ReadInternalAsync(pageId, ct);
            
            if (node.Header.NodeType == NodeType.Leaf)
            {
                var leaf = await _reader.ReadLeafAsync(pageId, ct);
                if (leaf.Header.KeyCount > 0)
                {
                    return (true, leaf.GetKeys(_maxDegree)[0], leaf.GetValues(_maxDegree)[0]);
                }
                return (false, default, default);
            }
            
            // Follow leftmost child
            pageId = node.GetChildPageIds(_maxDegree)[0];
        }
    }

    /// <summary>
    /// Gets the maximum key in the tree.
    /// </summary>
    public async ValueTask<(bool found, TKey key, TValue value)> MaxAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        var pageId = _rootPageId;
        
        // Navigate to rightmost leaf
        while (true)
        {
            var node = await _reader.ReadInternalAsync(pageId, ct);
            
            if (node.Header.NodeType == NodeType.Leaf)
            {
                var leaf = await _reader.ReadLeafAsync(pageId, ct);
                if (leaf.Header.KeyCount > 0)
                {
                    var idx = leaf.Header.KeyCount - 1;
                    return (true, leaf.GetKeys(_maxDegree)[idx], leaf.GetValues(_maxDegree)[idx]);
                }
                return (false, default, default);
            }
            
            // Follow rightmost child
            pageId = node.GetChildPageIds(_maxDegree)[node.Header.KeyCount];
        }
    }

    /// <summary>
    /// Enumerates all key-value pairs in ascending key order.
    /// </summary>
    public async IAsyncEnumerable<(TKey key, TValue value)> InOrderAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        // Navigate to leftmost leaf
        var pageId = _rootPageId;
        
        while (true)
        {
            var node = await _reader.ReadInternalAsync(pageId, ct);
            
            if (node.Header.NodeType == NodeType.Leaf)
            {
                break;
            }
            
            pageId = node.GetChildPageIds(_maxDegree)[0];
        }
        
        // Scan all leaves
        while (pageId >= 0)
        {
            var leaf = await _reader.ReadLeafAsync(pageId, ct);
            
            // Access span elements immediately - cannot hold Span<T> across await boundary
            for (int i = 0; i < leaf.Header.KeyCount; i++)
            {
                yield return (leaf.GetKey(i, _maxDegree), leaf.GetValue(i, _maxDegree));
            }
            
            pageId = leaf.Header.NextSiblingPageId;
        }
    }

    /// <summary>
    /// Scans key-value pairs within the specified range [from, to] inclusive.
    /// </summary>
    public async IAsyncEnumerable<(TKey key, TValue value)> ScanAsync(
        TKey from, 
        TKey to, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();
        
        // Find starting leaf
        var pageId = await FindLeafPageAsync(from, ct);
        
        while (pageId >= 0)
        {
            var leaf = await _reader.ReadLeafAsync(pageId, ct);
            
            bool foundEnd = false;
            
            for (int i = 0; i < leaf.Header.KeyCount; i++)
            {
                var key = leaf.GetKey(i, _maxDegree);
                
                if (key.CompareTo(from) < 0)
                    continue;
                
                if (key.CompareTo(to) > 0)
                {
                    foundEnd = true;
                    break;
                }
                
                yield return (key, leaf.GetValue(i, _maxDegree));
            }
            
            if (foundEnd)
                break;
            
            pageId = leaf.Header.NextSiblingPageId;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Finds the leaf page that should contain the given key.
    /// </summary>
    private async ValueTask<long> FindLeafPageAsync(TKey key, CancellationToken ct)
    {
        var pageId = _rootPageId;
        
        while (true)
        {
            // Read the node - we read as internal first to check type
            var node = await _reader.ReadInternalAsync(pageId, ct);
            
            if (node.Header.NodeType == NodeType.Leaf)
            {
                return pageId;
            }
            
            // Find the child page to follow
            var keys = node.GetKeys(_maxDegree);
            var childPageIds = node.GetChildPageIds(_maxDegree);
            
            int i = 0;
            while (i < node.Header.KeyCount && key.CompareTo(keys[i]) >= 0)
            {
                i++;
            }
            
            pageId = childPageIds[i];
        }
    }

    /// <summary>
    /// Inserts a key-value pair into a leaf node at the specified index.
    /// </summary>
    private void InsertIntoLeaf(ref BPlusLeafNode<TKey, TValue> leaf, int index, TKey key, TValue value)
    {
        var keys = leaf.GetKeys(_maxDegree);
        var values = leaf.GetValues(_maxDegree);
        var count = leaf.Header.KeyCount;
        
        // Shift elements to make room
        for (int i = count; i > index; i--)
        {
            keys[i] = keys[i - 1];
            values[i] = values[i - 1];
        }
        
        keys[index] = key;
        values[index] = value;
        leaf.Header.KeyCount++;
    }

    /// <summary>
    /// Splits a full leaf node into two nodes.
    /// </summary>
    private async ValueTask SplitLeafAsync(long leafPageId, BPlusLeafNode<TKey, TValue> leaf, CancellationToken ct)
    {
        var newLeafPageId = await _storage.AllocatePageAsync(ct);
        
        // Create new leaf with upper half of keys
        var newLeaf = new BPlusLeafNode<TKey, TValue>
        {
            Header = new BPlusNodeHeader
            {
                NodeType = NodeType.Leaf,
                KeyCount = 0,
                NextSiblingPageId = leaf.Header.NextSiblingPageId
            }
        };
        
        var mid = leaf.Header.KeyCount / 2;
        var oldKeys = leaf.GetKeys(_maxDegree);
        var oldValues = leaf.GetValues(_maxDegree);
        var newKeys = newLeaf.GetKeys(_maxDegree);
        var newValues = newLeaf.GetValues(_maxDegree);
        
        // Copy upper half to new leaf
        for (int i = mid; i < leaf.Header.KeyCount; i++)
        {
            newKeys[i - mid] = oldKeys[i];
            newValues[i - mid] = oldValues[i];
            newLeaf.Header.KeyCount++;
        }
        
        // Update old leaf
        leaf.Header.KeyCount = (ushort)mid;
        leaf.Header.NextSiblingPageId = newLeafPageId;
        
        // Get separator key before await (Span cannot cross await boundary)
        var separatorKey = newLeaf.GetKey(0, _maxDegree);
        
        // Write both leaves
        await _reader.WriteLeafAsync(leafPageId, leaf, ct);
        await _reader.WriteLeafAsync(newLeafPageId, newLeaf, ct);
        
        // Insert new key into parent
        await InsertIntoParentAsync(leafPageId, separatorKey, newLeafPageId, ct);
    }

    /// <summary>
    /// Inserts a separator key and new child page into the parent node.
    /// </summary>
    private async ValueTask InsertIntoParentAsync(long leftPageId, TKey key, long rightPageId, CancellationToken ct)
    {
        if (leftPageId == _rootPageId)
        {
            // Create new root
            await CreateNewRootAsync(key, leftPageId, rightPageId, ct);
            return;
        }
        
        // Simplified - full implementation would maintain parent pointers
        // For now, we create a new root for any split (flattens the tree temporarily)
        // A complete implementation needs parent tracking during descent
        await CreateNewRootAsync(key, leftPageId, rightPageId, ct);
    }

    /// <summary>
    /// Creates a new root node with the given key and children.
    /// </summary>
    private async ValueTask CreateNewRootAsync(TKey key, long leftPageId, long rightPageId, CancellationToken ct)
    {
        var newRootPageId = await _storage.AllocatePageAsync(ct);
        
        var newRoot = new BPlusInternalNode<TKey>
        {
            Header = new BPlusNodeHeader
            {
                NodeType = NodeType.Internal,
                KeyCount = 1,
                NextSiblingPageId = -1
            }
        };
        
        newRoot.GetKeys(_maxDegree)[0] = key;
        newRoot.GetChildPageIds(_maxDegree)[0] = leftPageId;
        newRoot.GetChildPageIds(_maxDegree)[1] = rightPageId;
        
        await _reader.WriteInternalAsync(newRootPageId, newRoot, ct);
        _rootPageId = newRootPageId;
    }

    /// <summary>
    /// Performs binary search on a span of keys.
    /// </summary>
    private static int BinarySearch(Span<TKey> keys, TKey key)
    {
        int lo = 0;
        int hi = keys.Length - 1;
        
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            var cmp = keys[mid].CompareTo(key);
            
            if (cmp == 0)
                return mid;
            if (cmp < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }
        
        return ~lo;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PersistentBPlusTree<TKey, TValue>));
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Flushes all pending writes to the underlying storage.
    /// </summary>
    public async ValueTask FlushAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _storage.FlushAsync(ct);
    }

    /// <summary>
    /// Disposes the tree and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await FlushAsync();
            await _storage.DisposeAsync();
            _disposed = true;
        }
    }

    #endregion
}
