using System;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a Splay Tree, a self-balancing binary search tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree, must be comparable.</typeparam>
public class SplayTree<T> : ITree<T> where T : IComparable<T>
{
    /// <summary>
    /// Gets or sets the root of the tree.
    /// </summary>
    public SplayTreeNode<T> Root { get; private set; }

    /// <inheritdoc />
    public void Insert(T value)
    {
        if (Root == null)
        {
            Root = new SplayTreeNode<T>(value);
            return;
        }

        Root = Splay(Root, value);

        var cmp = value.CompareTo(Root.Value);
        if (cmp < 0)
        {
            var newNode = new SplayTreeNode<T>(value);
            newNode.Left = Root.Left;
            newNode.Right = Root;
            Root.Left = null;
            Root = newNode;
        }
        else if (cmp > 0)
        {
            var newNode = new SplayTreeNode<T>(value);
            newNode.Right = Root.Right;
            newNode.Left = Root;
            Root.Right = null;
            Root = newNode;
        }
    }

    /// <inheritdoc />
    public void Delete(T value)
    {
        if (Root == null) return;

        Root = Splay(Root, value);

        if (Root.Value.CompareTo(value) != 0) return;

        if (Root.Left == null)
        {
            Root = (SplayTreeNode<T>)Root.Right;
        }
        else
        {
            var newRoot = (SplayTreeNode<T>)Root.Left;
            newRoot = Splay(newRoot, value);
            newRoot.Right = Root.Right;
            Root = newRoot;
        }
    }

    /// <inheritdoc />
    public ITreeNode<T> Find(T value)
    {
        Root = Splay(Root, value);
        if (Root == null || Root.Value.CompareTo(value) != 0)
        {
            return null;
        }

        return Root;
    }

    private SplayTreeNode<T> Splay(SplayTreeNode<T> node, T value)
    {
        if (node == null) return null;

        var cmp1 = value.CompareTo(node.Value);

        if (cmp1 < 0)
        {
            if (node.Left == null) return node;
            var cmp2 = value.CompareTo(node.Left.Value);
            if (cmp2 < 0)
            {
                node.Left.Left = Splay((SplayTreeNode<T>)node.Left.Left, value);
                node = RightRotate(node);
            }
            else if (cmp2 > 0)
            {
                node.Left.Right = Splay((SplayTreeNode<T>)node.Left.Right, value);
                if (node.Left.Right != null)
                    node.Left = LeftRotate((SplayTreeNode<T>)node.Left);
            }
                
            return node.Left == null ? node : RightRotate(node);
        }
            
        if (cmp1 > 0)
        {
            if (node.Right == null) return node;

            var cmp2 = value.CompareTo(node.Right.Value);
            if (cmp2 < 0)
            {
                node.Right.Left = Splay((SplayTreeNode<T>)node.Right.Left, value);
                if (node.Right.Left != null)
                    node.Right = RightRotate((SplayTreeNode<T>)node.Right);
            }
            else if (cmp2 > 0)
            {
                node.Right.Right = Splay((SplayTreeNode<T>)node.Right.Right, value);
                node = LeftRotate(node);
            }
                
            return node.Right == null ? node : LeftRotate(node);
        }
            
        return node;
    }

    private SplayTreeNode<T> RightRotate(SplayTreeNode<T> x)
    {
        var y = (SplayTreeNode<T>)x.Left;
        x.Left = y.Right;
        y.Right = x;
        return y;
    }

    private SplayTreeNode<T> LeftRotate(SplayTreeNode<T> x)
    {
        var y = (SplayTreeNode<T>)x.Right;
        x.Right = y.Left;
        y.Left = x;
        return y;
    }
}