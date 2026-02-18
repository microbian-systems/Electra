using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a node in a Segment Tree for ITreeNode interface.
/// </summary>
public class SegmentTreeNodeWrapper : ITreeNode<int>
{
    private readonly SegmentTreeNode _node;

    public SegmentTreeNodeWrapper(SegmentTreeNode node)
    {
        _node = node;
    }

    public int Value
    {
        get => _node.Sum;
        set => throw new NotSupportedException("Cannot set value directly on SegmentTree node. Use Update instead.");
    }

    public IEnumerable<ITreeNode<int>> Children
    {
        get
        {
            if (_node.Left != null)
                yield return new SegmentTreeNodeWrapper(_node.Left);
            if (_node.Right != null)
                yield return new SegmentTreeNodeWrapper(_node.Right);
        }
    }
}

/// <summary>
/// Represents a Segment Tree for efficient range queries (sum in this case).
/// </summary>
public class SegmentTree : ITree<int>
{
    private readonly int[] _data;
    public SegmentTreeNode Root { get; private set; }

    public SegmentTree(int[] data)
    {
        _data = data;
        Root = Build(0, _data.Length - 1);
    }

    private SegmentTreeNode Build(int start, int end)
    {
        if (start > end)
        {
            return null;
        }

        var node = new SegmentTreeNode { Start = start, End = end };
        if (start == end)
        {
            node.Sum = _data[start];
        }
        else
        {
            var mid = start + (end - start) / 2;
            node.Left = Build(start, mid);
            node.Right = Build(mid + 1, end);
            node.Sum = (node.Left?.Sum ?? 0) + (node.Right?.Sum ?? 0);
        }
        return node;
    }

    public void Update(int index, int value)
    {
        if (index < 0 || index >= _data.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        _data[index] = value;
        Update(Root, index, value);
    }

    private void Update(SegmentTreeNode node, int index, int value)
    {
        if (node.Start == node.End)
        {
            node.Sum = value;
            return;
        }

        var mid = node.Start + (node.End - node.Start) / 2;
        if (index <= mid)
        {
            Update(node.Left, index, value);
        }
        else
        {
            Update(node.Right, index, value);
        }
        node.Sum = (node.Left?.Sum ?? 0) + (node.Right?.Sum ?? 0);
    }

    public int Query(int start, int end)
    {
        return Query(Root, start, end);
    }

    private int Query(SegmentTreeNode node, int start, int end)
    {
        if (node == null || start > node.End || end < node.Start)
        {
            return 0;
        }

        if (start <= node.Start && end >= node.End)
        {
            return node.Sum;
        }

        var mid = node.Start + (node.End - node.Start) / 2;
        var leftSum = Query(node.Left, start, end);
        var rightSum = Query(node.Right, start, end);

        return leftSum + rightSum;
    }

    /// <inheritdoc />
    /// <summary>
    /// Inserts a value at the specified index. Note: SegmentTree has fixed size,
    /// so this operation updates the value at the given index.
    /// </summary>
    public void Insert(int index)
    {
        // Segment tree doesn't support dynamic insertion
        // The value parameter represents the index to update to itself
        if (index < 0 || index >= _data.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        // No-op: value already exists at index
    }

    /// <inheritdoc />
    /// <summary>
    /// Deletes a value by setting it to 0 at the specified index.
    /// </summary>
    public void Delete(int index)
    {
        Update(index, 0);
    }

    /// <inheritdoc />
    /// <summary>
    /// Finds the node representing the sum at the given index range.
    /// The value parameter represents the index to find.
    /// </summary>
    public ITreeNode<int> Find(int index)
    {
        if (index < 0 || index >= _data.Length)
            return null;
        return new SegmentTreeNodeWrapper(Root);
    }
}