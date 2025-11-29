using System;

namespace Electra.DataStructures.Trees;

public class RedBlackTree<T> : ITree<T> where T : IComparable<T>
{
    public RedBlackTreeNode<T> Root { get; private set; }

    public void Insert(T value)
    {
        Root = Insert(Root, value);
        Root.Color = NodeColor.Black;
    }

    private RedBlackTreeNode<T> Insert(RedBlackTreeNode<T> node, T value)
    {
        if (node == null)
            return new RedBlackTreeNode<T>(value);

        var compare = value.CompareTo(node.Value);

        if (compare < 0)
            node.Left = Insert(Node(node.Left), value);
        else if (compare > 0)
            node.Right = Insert(Node(node.Right), value);

        // Fix right-leaning reds
        if (IsRed(Node(node.Right)) && !IsRed(Node(node.Left)))
            node = LeftRotate(node);

        // Fix two reds in a row
        if (IsRed(Node(node.Left)) && IsRed(Node(Node(node.Left).Left)))
            node = RightRotate(node);

        // Split 4-nodes
        if (IsRed(Node(node.Left)) && IsRed(Node(node.Right)))
            FlipColors(node);

        return node;
    }

    public void Delete(T value)
    {
        if (Root == null) return;

        // Ensure the root is red so we can push a red link down if needed
        if (!IsRed(Node(Root.Left)) && !IsRed(Node(Root.Right)))
            Root.Color = NodeColor.Red;

        Root = Delete(Root, value);

        if (Root != null)
            Root.Color = NodeColor.Black;
    }

    private RedBlackTreeNode<T> Delete(RedBlackTreeNode<T> node, T value)
    {
        if (value.CompareTo(node.Value) < 0)
        {
            // If moving left, ensure the left child is not a 2-node
            if (node.Left != null && !IsRed(Node(node.Left)) && !IsRed(Node(Node(node.Left).Left)))
                node = MoveRedLeft(node);
            
            node.Left = Delete(Node(node.Left), value);
        }
        else
        {
            if (IsRed(Node(node.Left)))
                node = RightRotate(node);

            if (value.CompareTo(node.Value) == 0 && node.Right == null)
                return null; // Node to delete found at leaf

            // If moving right, ensure right child is not a 2-node
            if (node.Right != null && !IsRed(Node(node.Right)) && !IsRed(Node(Node(node.Right).Left)))
                node = MoveRedRight(node);

            if (value.CompareTo(node.Value) == 0)
            {
                var minNode = Min(Node(node.Right));
                node.Value = minNode.Value;
                node.Right = DeleteMin(Node(node.Right));
            }
            else
            {
                node.Right = Delete(Node(node.Right), value);
            }
        }

        return Balance(node);
    }

    private RedBlackTreeNode<T> DeleteMin(RedBlackTreeNode<T> node)
    {
        if (node.Left == null)
            return null;

        if (!IsRed(Node(node.Left)) && !IsRed(Node(Node(node.Left).Left)))
            node = MoveRedLeft(node);

        node.Left = DeleteMin(Node(node.Left));
        return Balance(node);
    }

    private RedBlackTreeNode<T> Min(RedBlackTreeNode<T> node)
    {
        while (node.Left != null)
            node = Node(node.Left);
        return node;
    }

    private RedBlackTreeNode<T> MoveRedLeft(RedBlackTreeNode<T> node)
    {
        FlipColors(node);
        
        // Safety check: Ensure Right exists before checking Right.Left
        var right = Node(node.Right);
        if (right != null && IsRed(Node(right.Left)))
        {
            node.Right = RightRotate(right);
            node = LeftRotate(node);
            FlipColors(node);
        }
        return node;
    }

    private RedBlackTreeNode<T> MoveRedRight(RedBlackTreeNode<T> node)
    {
        FlipColors(node);
        
        // Safety check: Ensure Left exists before checking Left.Left
        var left = Node(node.Left);
        if (left != null && IsRed(Node(left.Left)))
        {
            node = RightRotate(node);
            FlipColors(node);
        }
        return node;
    }

    private RedBlackTreeNode<T> Balance(RedBlackTreeNode<T> node)
    {
        if (IsRed(Node(node.Right)))
            node = LeftRotate(node);
        
        if (IsRed(Node(node.Left)) && IsRed(Node(Node(node.Left).Left)))
            node = RightRotate(node);
        
        if (IsRed(Node(node.Left)) && IsRed(Node(node.Right)))
            FlipColors(node);

        return node;
    }

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
                node = Node(node.Left);
            else if (compare > 0)
                node = Node(node.Right);
            else
                return node;
        }
        return null;
    }

    private bool IsRed(RedBlackTreeNode<T> node)
    {
        return node != null && node.Color == NodeColor.Red;
    }

    /// <summary>
    /// Inverts the colors of the node and its children.
    /// Works for both splitting 4-nodes (insert) and merging 3-nodes (delete).
    /// </summary>
    private void FlipColors(RedBlackTreeNode<T> node)
    {
        // Toggle Node Color
        node.Color = node.Color == NodeColor.Red ? NodeColor.Black : NodeColor.Red;
        
        // Toggle Left Child Color
        if (node.Left != null)
        {
            var left = Node(node.Left);
            left.Color = left.Color == NodeColor.Red ? NodeColor.Black : NodeColor.Red;
        }

        // Toggle Right Child Color
        if (node.Right != null)
        {
            var right = Node(node.Right);
            right.Color = right.Color == NodeColor.Red ? NodeColor.Black : NodeColor.Red;
        }
    }

    private RedBlackTreeNode<T> LeftRotate(RedBlackTreeNode<T> node)
    {
        var x = Node(node.Right);
        node.Right = x.Left;
        x.Left = node;
        x.Color = node.Color;
        node.Color = NodeColor.Red;
        return x;
    }

    private RedBlackTreeNode<T> RightRotate(RedBlackTreeNode<T> node)
    {
        var x = Node(node.Left);
        node.Left = x.Right;
        x.Right = node;
        x.Color = node.Color;
        node.Color = NodeColor.Red;
        return x;
    }

    // Helper to reduce casting noise and safely handle ITreeNode interface
    private RedBlackTreeNode<T> Node(ITreeNode<T> node)
    {
        return (RedBlackTreeNode<T>)node;
    }
}