using System;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a Binary Search Tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree, must be comparable.</typeparam>
public class BinarySearchTree<T> : ITree<T> where T : IComparable<T>
{
    /// <summary>
    /// Gets or sets the root of the tree.
    /// </summary>
    public BinarySearchTreeNode<T> Root { get; private set; }

    /// <inheritdoc />
    public void Insert(T value)
    {
        Root = Insert(Root, value);
    }

    private BinarySearchTreeNode<T> Insert(BinarySearchTreeNode<T> node, T value)
    {
        if (node == null)
        {
            return new BinarySearchTreeNode<T>(value);
        }

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            node.Left = Insert((BinarySearchTreeNode<T>)node.Left, value);
        }
        else if (compare > 0)
        {
            node.Right = Insert((BinarySearchTreeNode<T>)node.Right, value);
        }

        return node;
    }

    /// <inheritdoc />
    public void Delete(T value)
    {
        Root = Delete(Root, value);
    }

    private BinarySearchTreeNode<T> Delete(BinarySearchTreeNode<T> node, T value)
    {
        if (node == null)
        {
            return null;
        }

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            node.Left = Delete((BinarySearchTreeNode<T>)node.Left, value);
        }
        else if (compare > 0)
        {
            node.Right = Delete((BinarySearchTreeNode<T>)node.Right, value);
        }
        else
        {
            if (node.Left == null)
            {
                return (BinarySearchTreeNode<T>)node.Right;
            }
                
            if (node.Right == null)
            {
                return (BinarySearchTreeNode<T>)node.Left;
            }

            node.Value = MinValue((BinarySearchTreeNode<T>)node.Right);
            node.Right = Delete((BinarySearchTreeNode<T>)node.Right, node.Value);
        }

        return node;
    }

    private T MinValue(BinarySearchTreeNode<T> node)
    {
        var minValue = node.Value;
        while (node.Left != null)
        {
            minValue = node.Left.Value;
            node = (BinarySearchTreeNode<T>)node.Left;
        }
        return minValue;
    }

    /// <inheritdoc />
    public ITreeNode<T> Find(T value)
    {
        return Find(Root, value);
    }

    private ITreeNode<T> Find(BinarySearchTreeNode<T> node, T value)
    {
        if (node == null)
        {
            return null;
        }

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            return Find((BinarySearchTreeNode<T>)node.Left, value);
        }
            
        return compare > 0 ? Find((BinarySearchTreeNode<T>)node.Right, value) : node;
    }
}