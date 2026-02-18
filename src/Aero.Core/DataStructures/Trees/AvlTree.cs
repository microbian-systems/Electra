using System;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents an AVL Tree, a self-balancing binary search tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree, must be comparable.</typeparam>
public class AvlTree<T> : ITree<T> where T : IComparable<T>
{
    /// <summary>
    /// Gets or sets the root of the tree.
    /// </summary>
    public AvlTreeNode<T> Root { get; private set; }

    /// <inheritdoc />
    public void Insert(T value)
    {
        Root = Insert(Root, value);
    }

    private AvlTreeNode<T> Insert(AvlTreeNode<T> node, T value)
    {
        if (node == null)
        {
            return new AvlTreeNode<T>(value);
        }

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            node.Left = Insert((AvlTreeNode<T>)node.Left, value);
        }
        else if (compare > 0)
        {
            node.Right = Insert((AvlTreeNode<T>)node.Right, value);
        }
        else
        {
            // Duplicate values are not allowed in this implementation
            return node;
        }

        node.Height = 1 + Math.Max(Height((AvlTreeNode<T>)node.Left), Height((AvlTreeNode<T>)node.Right));

        return Balance(node);
    }

    /// <inheritdoc />
    public void Delete(T value)
    {
        Root = Delete(Root, value);
    }

    private AvlTreeNode<T> Delete(AvlTreeNode<T> node, T value)
    {
        if (node == null)
        {
            return null;
        }

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            node.Left = Delete((AvlTreeNode<T>)node.Left, value);
        }
        else if (compare > 0)
        {
            node.Right = Delete((AvlTreeNode<T>)node.Right, value);
        }
        else
        {
            if (node.Left == null || node.Right == null)
            {
                node = (AvlTreeNode<T>)(node.Left ?? node.Right);
            }
            else
            {
                var temp = MinValueNode((AvlTreeNode<T>)node.Right);
                node.Value = temp.Value;
                node.Right = Delete((AvlTreeNode<T>)node.Right, temp.Value);
            }
        }

        if (node == null)
        {
            return null;
        }

        node.Height = 1 + Math.Max(Height((AvlTreeNode<T>)node.Left), Height((AvlTreeNode<T>)node.Right));

        return Balance(node);
    }

    /// <inheritdoc />
    public ITreeNode<T> Find(T value)
    {
        return Find(Root, value);
    }

    private AvlTreeNode<T> Find(AvlTreeNode<T> node, T value)
    {
        if (node == null)
        {
            return null;
        }

        var compare = value.CompareTo(node.Value);
        if (compare < 0)
        {
            return Find((AvlTreeNode<T>)node.Left, value);
        }

        return compare > 0 ? Find((AvlTreeNode<T>)node.Right, value) : node;
    }

    private int Height(AvlTreeNode<T> node)
    {
        return node?.Height ?? 0;
    }

    private int BalanceFactor(AvlTreeNode<T> node)
    {
        return node == null ? 0 : Height((AvlTreeNode<T>)node.Left) - Height((AvlTreeNode<T>)node.Right);
    }

    private AvlTreeNode<T> Balance(AvlTreeNode<T> node)
    {
        var balance = BalanceFactor(node);

        // Left-Left case
        if (balance > 1 && BalanceFactor((AvlTreeNode<T>)node.Left) >= 0)
        {
            return RightRotate(node);
        }

        // Left-Right case
        if (balance > 1 && BalanceFactor((AvlTreeNode<T>)node.Left) < 0)
        {
            node.Left = LeftRotate((AvlTreeNode<T>)node.Left);
            return RightRotate(node);
        }

        // Right-Right case
        if (balance < -1 && BalanceFactor((AvlTreeNode<T>)node.Right) <= 0)
        {
            return LeftRotate(node);
        }

        // Right-Left case
        if (balance < -1 && BalanceFactor((AvlTreeNode<T>)node.Right) > 0)
        {
            node.Right = RightRotate((AvlTreeNode<T>)node.Right);
            return LeftRotate(node);
        }

        return node;
    }

    private AvlTreeNode<T> RightRotate(AvlTreeNode<T> y)
    {
        var x = (AvlTreeNode<T>)y.Left;
        var T2 = (AvlTreeNode<T>)x.Right;

        x.Right = y;
        y.Left = T2;

        y.Height = 1 + Math.Max(Height((AvlTreeNode<T>)y.Left), Height((AvlTreeNode<T>)y.Right));
        x.Height = 1 + Math.Max(Height((AvlTreeNode<T>)x.Left), Height((AvlTreeNode<T>)x.Right));

        return x;
    }

    private AvlTreeNode<T> LeftRotate(AvlTreeNode<T> x)
    {
        var y = (AvlTreeNode<T>)x.Right;
        var T2 = (AvlTreeNode<T>)y.Left;

        y.Left = x;
        x.Right = T2;

        x.Height = 1 + Math.Max(Height((AvlTreeNode<T>)x.Left), Height((AvlTreeNode<T>)x.Right));
        y.Height = 1 + Math.Max(Height((AvlTreeNode<T>)y.Left), Height((AvlTreeNode<T>)y.Right));

        return y;
    }

    private AvlTreeNode<T> MinValueNode(AvlTreeNode<T> node)
    {
        var current = node;
        while (current.Left != null)
        {
            current = (AvlTreeNode<T>)current.Left;
        }
        return current;
    }
}