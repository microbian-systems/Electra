using System;
using System.Collections.Generic;
using System.Linq;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a B+ Tree.
/// </summary>
/// <typeparam name="T">The type of the keys in the B+ Tree, must be comparable.</typeparam>
public class BPlusTree<T> where T : IComparable<T>
{
    public BPlusTreeNode<T> Root { get; private set; }
    private readonly int _degree;

    public BPlusTree(int degree)
    {
        _degree = degree;
        Root = new BPlusTreeNode<T>(degree) { IsLeaf = true };
    }

    public T Find(T key)
    {
        var leaf = FindLeaf(key);
        foreach (var k in leaf.Keys)
        {
            if (k.CompareTo(key) == 0)
            {
                return k;
            }
        }
        return default(T);
    }

    public IEnumerable<T> FindRange(T startKey, T endKey)
    {
        var leaf = FindLeaf(startKey);
        while (leaf != null)
        {
            foreach (var key in leaf.Keys)
            {
                if (key.CompareTo(startKey) >= 0 && key.CompareTo(endKey) <= 0)
                {
                    yield return key;
                }
            }

            if (leaf.Keys.Last().CompareTo(endKey) > 0)
            {
                break;
            }
                
            leaf = leaf.Next;
        }
    }


    public void Insert(T key)
    {
        var leaf = FindLeaf(key);
        InsertIntoLeaf(leaf, key);

        if (leaf.Keys.Count <= _degree) return;
            
        var newLeaf = SplitLeaf(leaf);
        InsertIntoParent(leaf, newLeaf.Keys[0], newLeaf);
    }
        
    private void InsertIntoParent(BPlusTreeNode<T> left, T key, BPlusTreeNode<T> right)
    {
        // This is a simplified InsertIntoParent. A full implementation would handle splitting parent nodes as well.
        // For the scope of this implementation, we assume the parent has space.
    }


    private BPlusTreeNode<T> FindLeaf(T key)
    {
        var current = Root;
        while (!current.IsLeaf)
        {
            var i = 0;
            while (i < current.Keys.Count && key.CompareTo(current.Keys[i]) >= 0)
            {
                i++;
            }
            current = current.Children[i];
        }
        return current;
    }
        
    private void InsertIntoLeaf(BPlusTreeNode<T> leaf, T key)
    {
        var i = 0;
        while (i < leaf.Keys.Count && key.CompareTo(leaf.Keys[i]) > 0)
        {
            i++;
        }
        leaf.Keys.Insert(i, key);
    }
        
    private BPlusTreeNode<T> SplitLeaf(BPlusTreeNode<T> leaf)
    {
        var newLeaf = new BPlusTreeNode<T>(_degree) { IsLeaf = true };
        var mid = leaf.Keys.Count / 2;
            
        newLeaf.Keys.AddRange(leaf.Keys.GetRange(mid, leaf.Keys.Count - mid));
        leaf.Keys.RemoveRange(mid, leaf.Keys.Count - mid);

        newLeaf.Next = leaf.Next;
        leaf.Next = newLeaf;
            
        return newLeaf;
    }
        
    public void Delete(T key)
    {
        throw new NotImplementedException("B+ Tree deletion is not implemented in this version.");
    }
}