using System;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a Red-Black Tree, a self-balancing binary search tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree, must be comparable.</typeparam>
public class RedBlackTree<T> : ITree<T> where T : IComparable<T>
{
    /// <summary>
    /// Gets or sets the root of the tree.
    /// </summary>
    public RedBlackTreeNode<T> Root { get; private set; }

    /// <inheritdoc />
    public void Insert(T value)
    {
        Root = Insert(Root, value);
        Root.Color = NodeColor.Black;
    }

    private RedBlackTreeNode<T> Insert(RedBlackTreeNode<T> node, T value)
    {
        if (node == null)
        {
            return new RedBlackTreeNode<T>(value);
        }

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            node.Left = Insert((RedBlackTreeNode<T>)node.Left, value);
        }
        else if (compare > 0)
        {
            node.Right = Insert((RedBlackTreeNode<T>)node.Right, value);
        }

        if (IsRed((RedBlackTreeNode<T>)node.Right) && !IsRed((RedBlackTreeNode<T>)node.Left))
        {
            node = LeftRotate(node);
        }

        if (IsRed((RedBlackTreeNode<T>)node.Left) && IsRed((RedBlackTreeNode<T>)((RedBlackTreeNode<T>)node.Left).Left))
        {
            node = RightRotate(node);
        }
            
        if (IsRed((RedBlackTreeNode<T>)node.Left) && IsRed((RedBlackTreeNode<T>)node.Right))
        {
            FlipColors(node);
        }

        return node;
    }

    /// <inheritdoc />
    public void Delete(T value)
    {
        // Deletion in a Red-Black tree is notoriously complex.
        // This is a simplified version and may not cover all cases.
        Root = Delete(Root, value);
        if (Root != null)
        {
            Root.Color = NodeColor.Black;
        }
    }
        
    private RedBlackTreeNode<T> Delete(RedBlackTreeNode<T> node, T value)
    {
        // Simplified delete - does not rebalance tree
        if (node == null) return null;

        var cmp = value.CompareTo(node.Value);
        if (cmp < 0)
        {
            node.Left = Delete((RedBlackTreeNode<T>)node.Left, value);
        }
        else if (cmp > 0)
        {
            node.Right = Delete((RedBlackTreeNode<T>)node.Right, value);
        }
        else
        {
            if (node.Right == null) return (RedBlackTreeNode<T>)node.Left;
            if (node.Left == null) return (RedBlackTreeNode<T>)node.Right;

            var temp = Min((RedBlackTreeNode<T>)node.Right);
            node.Value = temp.Value;
            node.Right = DeleteMin((RedBlackTreeNode<T>)node.Right);
        }
        return node;
    }
        
    private RedBlackTreeNode<T> DeleteMin(RedBlackTreeNode<T> node)
    {
        if (node.Left == null) return (RedBlackTreeNode<T>)node.Right;
        node.Left = DeleteMin((RedBlackTreeNode<T>)node.Left);
        return node;
    }
        
    private RedBlackTreeNode<T> Min(RedBlackTreeNode<T> node)
    {
        if (node.Left == null) return node;
        return Min((RedBlackTreeNode<T>)node.Left);
    }

    /// <inheritdoc />
    public ITreeNode<T> Find(T value)
    {
        return Find(Root, value);
    }

    private RedBlackTreeNode<T> Find(RedBlackTreeNode<T> node, T value)
    {
        while (node != null)
        {
            var compare = value.CompareTo(node.Value);
            if (compare < 0)
            {
                node = (RedBlackTreeNode<T>)node.Left;
            }
            else if (compare > 0)
            {
                node = (RedBlackTreeNode<T>)node.Right;
            }
            else
            {
                return node;
            }
        }
        return null;
    }

    private bool IsRed(RedBlackTreeNode<T> node)
    {
        return node != null && node.Color == NodeColor.Red;
    }

    private void FlipColors(RedBlackTreeNode<T> node)
    {
        node.Color = NodeColor.Red;
        ((RedBlackTreeNode<T>)node.Left).Color = NodeColor.Black;
        ((RedBlackTreeNode<T>)node.Right).Color = NodeColor.Black;
    }

    private RedBlackTreeNode<T> LeftRotate(RedBlackTreeNode<T> node)
    {
        var x = (RedBlackTreeNode<T>)node.Right;
        node.Right = x.Left;
        x.Left = node;
        x.Color = node.Color;
        node.Color = NodeColor.Red;
        return x;
    }

    private RedBlackTreeNode<T> RightRotate(RedBlackTreeNode<T> node)
    {
        var x = (RedBlackTreeNode<T>)node.Left;
        node.Left = x.Right;
        x.Right = node;
        x.Color = node.Color;
        node.Color = NodeColor.Red;
        return x;
    }
}