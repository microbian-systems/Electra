using System;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a Treap, a randomized binary search tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree, must be comparable.</typeparam>
public class Treap<T> : ITree<T> where T : IComparable<T>
{
    private readonly Random _random = new();
        
    /// <summary>
    /// Gets or sets the root of the tree.
    /// </summary>
    public TreapNode<T> Root { get; private set; }

    /// <inheritdoc />
    public void Insert(T value)
    {
        Root = Insert(Root, value, _random.Next());
    }

    private TreapNode<T> Insert(TreapNode<T> node, T value, int priority)
    {
        if (node == null)
        {
            return new TreapNode<T>(value, priority);
        }

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            node.Left = Insert((TreapNode<T>)node.Left, value, priority);
            if (((TreapNode<T>)node.Left).Priority > node.Priority)
            {
                node = RightRotate(node);
            }
        }
        else if (compare > 0)
        {
            node.Right = Insert((TreapNode<T>)node.Right, value, priority);
            if (((TreapNode<T>)node.Right).Priority > node.Priority)
            {
                node = LeftRotate(node);
            }
        }
            
        return node;
    }

    /// <inheritdoc />
    public void Delete(T value)
    {
        Root = Delete(Root, value);
    }
        
    private TreapNode<T> Delete(TreapNode<T> node, T value)
    {
        if (node == null) return null;

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            node.Left = Delete((TreapNode<T>)node.Left, value);
        }
        else if (compare > 0)
        {
            node.Right = Delete((TreapNode<T>)node.Right, value);
        }
        else
        {
            if (node.Left == null) return (TreapNode<T>)node.Right;
            if (node.Right == null) return (TreapNode<T>)node.Left;

            if (((TreapNode<T>)node.Left).Priority > ((TreapNode<T>)node.Right).Priority)
            {
                node = RightRotate(node);
                node.Right = Delete((TreapNode<T>)node.Right, value);
            }
            else
            {
                node = LeftRotate(node);
                node.Left = Delete((TreapNode<T>)node.Left, value);
            }
        }

        return node;
    }


    /// <inheritdoc />
    public ITreeNode<T> Find(T value)
    {
        return Find(Root, value);
    }
        
    private TreapNode<T> Find(TreapNode<T> node, T value)
    {
        if (node == null) return null;

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            return Find((TreapNode<T>)node.Left, value);
        }
            
        return compare > 0 ? Find((TreapNode<T>)node.Right, value) : node;
    }
        
    private TreapNode<T> RightRotate(TreapNode<T> y)
    {
        var x = (TreapNode<T>)y.Left;
        var T2 = (TreapNode<T>)x.Right;

        x.Right = y;
        y.Left = T2;

        return x;
    }

    private TreapNode<T> LeftRotate(TreapNode<T> x)
    {
        var y = (TreapNode<T>)x.Right;
        var T2 = (TreapNode<T>)y.Left;

        y.Left = x;
        x.Right = T2;

        return y;
    }
}