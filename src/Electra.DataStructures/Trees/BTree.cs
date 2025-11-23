using System;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a B-Tree.
/// </summary>
/// <typeparam name="T">The type of the keys in the B-Tree, must be comparable.</typeparam>
public class BTree<T> where T : IComparable<T>
{
    public BTreeNode<T> Root { get; private set; }
    private readonly int _degree; // Minimum degree

    public BTree(int degree)
    {
        _degree = degree;
        Root = new BTreeNode<T>(degree);
    }

    public bool Find(T key)
    {
        return Find(Root, key);
    }

    private bool Find(BTreeNode<T> node, T key)
    {
        var i = 0;
        while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) > 0)
        {
            i++;
        }

        if (i < node.Keys.Count && key.CompareTo(node.Keys[i]) == 0)
        {
            return true;
        }

        return !node.IsLeaf && Find(node.Children[i], key);
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
        // Deletion in B-Trees is complex and involves merging or redistributing keys.
        // This is a placeholder for a future implementation.
        throw new NotImplementedException("B-Tree deletion is not implemented in this version.");
    }
}