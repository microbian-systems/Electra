using System;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a B-Tree node wrapper for ITreeNode interface.
/// </summary>
public class BTreeNodeWrapper<T> : ITreeNode<T>
{
    private readonly BTreeNode<T> _node;
    private readonly int _keyIndex;

    public BTreeNodeWrapper(BTreeNode<T> node, int keyIndex)
    {
        _node = node;
        _keyIndex = keyIndex;
    }

    public T Value
    {
        get => _node.Keys[_keyIndex];
        set => _node.Keys[_keyIndex] = value;
    }

    public IEnumerable<ITreeNode<T>> Children
    {
        get
        {
            if (_node.IsLeaf) yield break;
            for (int i = 0; i < _node.Children.Count; i++)
            {
                var child = _node.Children[i];
                for (int j = 0; j < child.Keys.Count; j++)
                {
                    yield return new BTreeNodeWrapper<T>(child, j);
                }
            }
        }
    }
}

/// <summary>
/// Represents a B-Tree.
/// </summary>
/// <typeparam name="T">The type of the keys in the B-Tree, must be comparable.</typeparam>
public class BTree<T> : ITree<T> where T : IComparable<T>
{
    public BTreeNode<T> Root { get; private set; }
    private readonly int _degree; // Minimum degree

    public BTree(int degree)
    {
        _degree = degree;
        Root = new BTreeNode<T>(degree);
    }

    /// <summary>
    /// Finds a key in the B-Tree and returns true if found.
    /// </summary>
    public bool Contains(T key)
    {
        return FindNode(Root, key) != null;
    }

    private (BTreeNode<T> node, int index)? FindNode(BTreeNode<T> node, T key)
    {
        var i = 0;
        while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) > 0)
        {
            i++;
        }

        if (i < node.Keys.Count && key.CompareTo(node.Keys[i]) == 0)
        {
            return (node, i);
        }

        if (node.IsLeaf) return null;
        return FindNode(node.Children[i], key);
    }

    /// <inheritdoc />
    public ITreeNode<T> Find(T key)
    {
        var result = FindNode(Root, key);
        return result.HasValue ? new BTreeNodeWrapper<T>(result.Value.node, result.Value.index) : null;
    }

    public void Insert(T key)
    {
        var root = Root;
        if (root.Keys.Count == 2 * _degree - 1)
        {
            var newRoot = new BTreeNode<T>(_degree);
            Root = newRoot;
            newRoot.Children.Add(root);
            SplitChild(newRoot, 0);
            InsertNonFull(newRoot, key);
        }
        else
        {
            InsertNonFull(root, key);
        }
    }

    private void InsertNonFull(BTreeNode<T> node, T key)
    {
        var i = node.Keys.Count - 1;
        if (node.IsLeaf)
        {
            node.Keys.Add(default(T));
            while (i >= 0 && key.CompareTo(node.Keys[i]) < 0)
            {
                node.Keys[i + 1] = node.Keys[i];
                i--;
            }
            node.Keys[i + 1] = key;
        }
        else
        {
            while (i >= 0 && key.CompareTo(node.Keys[i]) < 0)
            {
                i--;
            }
            i++;
            if (node.Children[i].Keys.Count == 2 * _degree - 1)
            {
                SplitChild(node, i);
                if (key.CompareTo(node.Keys[i]) > 0)
                {
                    i++;
                }
            }
            InsertNonFull(node.Children[i], key);
        }
    }

    private void SplitChild(BTreeNode<T> parentNode, int childIndex)
    {
        var fullNode = parentNode.Children[childIndex];
        var newNode = new BTreeNode<T>(_degree);
            
        parentNode.Keys.Insert(childIndex, fullNode.Keys[_degree - 1]);
        parentNode.Children.Insert(childIndex + 1, newNode);

        newNode.Keys.AddRange(fullNode.Keys.GetRange(_degree, _degree - 1));
        fullNode.Keys.RemoveRange(_degree - 1, _degree);

        if (!fullNode.IsLeaf)
        {
            newNode.Children.AddRange(fullNode.Children.GetRange(_degree, _degree));
            fullNode.Children.RemoveRange(_degree, _degree);
        }
    }
        
    public void Delete(T key)
    {
        Remove(Root, key);

        if (Root.Keys.Count == 0 && !Root.IsLeaf)
        {
            Root = Root.Children[0];
        }
    }

    private void Remove(BTreeNode<T> node, T key)
    {
        int idx = FindKey(node, key);

        if (idx < node.Keys.Count && node.Keys[idx].CompareTo(key) == 0)
        {
            if (node.IsLeaf)
                RemoveFromLeaf(node, idx);
            else
                RemoveFromInternalNode(node, idx);
        }
        else
        {
            if (node.IsLeaf) return;

            bool flag = (idx == node.Keys.Count);

            if (node.Children[idx].Keys.Count < _degree)
                Fill(node, idx);

            if (flag && idx > node.Keys.Count)
                Remove(node.Children[idx - 1], key);
            else
                Remove(node.Children[idx], key);
        }
    }

    private void RemoveFromLeaf(BTreeNode<T> node, int idx)
    {
        node.Keys.RemoveAt(idx);
    }

    private void RemoveFromInternalNode(BTreeNode<T> node, int idx)
    {
        T key = node.Keys[idx];

        if (node.Children[idx].Keys.Count >= _degree)
        {
            T pred = GetPred(node, idx);
            node.Keys[idx] = pred;
            Remove(node.Children[idx], pred);
        }
        else if (node.Children[idx + 1].Keys.Count >= _degree)
        {
            T succ = GetSucc(node, idx);
            node.Keys[idx] = succ;
            Remove(node.Children[idx + 1], succ);
        }
        else
        {
            Merge(node, idx);
            Remove(node.Children[idx], key);
        }
    }

    private T GetPred(BTreeNode<T> node, int idx)
    {
        BTreeNode<T> cur = node.Children[idx];
        while (!cur.IsLeaf)
            cur = cur.Children[cur.Children.Count - 1];
        return cur.Keys[cur.Keys.Count - 1];
    }

    private T GetSucc(BTreeNode<T> node, int idx)
    {
        BTreeNode<T> cur = node.Children[idx + 1];
        while (!cur.IsLeaf)
            cur = cur.Children[0];
        return cur.Keys[0];
    }

    private void Fill(BTreeNode<T> node, int idx)
    {
        if (idx != 0 && node.Children[idx - 1].Keys.Count >= _degree)
            BorrowFromPrev(node, idx);
        else if (idx != node.Keys.Count && node.Children[idx + 1].Keys.Count >= _degree)
            BorrowFromNext(node, idx);
        else
        {
            if (idx != node.Keys.Count)
                Merge(node, idx);
            else
                Merge(node, idx - 1);
        }
    }

    private void BorrowFromPrev(BTreeNode<T> node, int idx)
    {
        BTreeNode<T> child = node.Children[idx];
        BTreeNode<T> sibling = node.Children[idx - 1];

        child.Keys.Insert(0, node.Keys[idx - 1]);

        if (!child.IsLeaf)
            child.Children.Insert(0, sibling.Children[sibling.Children.Count - 1]);

        node.Keys[idx - 1] = sibling.Keys[sibling.Keys.Count - 1];

        sibling.Keys.RemoveAt(sibling.Keys.Count - 1);
        if (!sibling.IsLeaf)
            sibling.Children.RemoveAt(sibling.Children.Count - 1);
    }

    private void BorrowFromNext(BTreeNode<T> node, int idx)
    {
        BTreeNode<T> child = node.Children[idx];
        BTreeNode<T> sibling = node.Children[idx + 1];

        child.Keys.Add(node.Keys[idx]);

        if (!child.IsLeaf)
            child.Children.Add(sibling.Children[0]);

        node.Keys[idx] = sibling.Keys[0];

        sibling.Keys.RemoveAt(0);
        if (!sibling.IsLeaf)
            sibling.Children.RemoveAt(0);
    }

    private void Merge(BTreeNode<T> node, int idx)
    {
        BTreeNode<T> child = node.Children[idx];
        BTreeNode<T> sibling = node.Children[idx + 1];

        child.Keys.Add(node.Keys[idx]);

        for (int i = 0; i < sibling.Keys.Count; ++i)
            child.Keys.Add(sibling.Keys[i]);

        if (!child.IsLeaf)
        {
            for (int i = 0; i < sibling.Children.Count; ++i)
                child.Children.Add(sibling.Children[i]);
        }

        node.Keys.RemoveAt(idx);
        node.Children.RemoveAt(idx + 1);
    }

    private int FindKey(BTreeNode<T> node, T key)
    {
        int idx = 0;
        while (idx < node.Keys.Count && node.Keys[idx].CompareTo(key) < 0)
            ++idx;
        return idx;
    }
}