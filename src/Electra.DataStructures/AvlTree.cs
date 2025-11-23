using System;

namespace Electra.DataStructures;

public class AvlTree<T> where T : IComparable<T>
{
    private Node _root;
    private int _size = 0;

    // Inner class representing a node in the AVL tree
    private record Node(T key)
    {
        public T Key { get; set; } = key;
        public int Height { get; set; } = 1; // Default height is 1
        public Node Left { get; set; }
        public Node Right { get; set; }
    }

    // ===========================
    // PUBLIC API
    // ===========================

    // Insert a new key into the AVL tree
    public void Insert(T key)
    {
        _root = InsertRecursive(_root, key);
    }

    // Remove a key from the AVL tree. Returns true if found and removed.
    public bool Remove(T key)
    {
        var originalSize = _size;
        _root = DeleteRecursive(_root, key);
        return _size < originalSize;
    }

    // Search for a key efficiently (Iterative)
    public bool Contains(T key)
    {
        var current = _root;
        while (current != null)
        {
            var comparisonResult = key.CompareTo(current.Key);
            if (comparisonResult == 0)
            {
                return true; // Key found
            }
            else if (comparisonResult < 0)
            {
                current = current.Left;
            }
            else
            {
                current = current.Right;
            }
        }

        return false; // Key not found
    }

    public int Size()
    {
        return _size;
    }

    public void PrintInOrder()
    {
        PrintInOrderRecursive(_root);
        Console.WriteLine();
    }

    // ===========================
    // HELPER METHODS & ROTATIONS
    // ===========================

    private int Height(Node node)
    {
        return node == null ? 0 : node.Height;
    }

    private void UpdateHeight(Node node)
    {
        if (node != null)
        {
            node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
        }
    }

    private int BalanceFactor(Node node)
    {
        return node == null ? 0 : Height(node.Left) - Height(node.Right);
    }

    // Helper to find the node with the minimum value in a subtree
    private Node GetMinValueNode(Node node)
    {
        var current = node;
        while (current.Left != null)
        {
            current = current.Left;
        }

        return current;
    }

    private Node LeftRotate(Node node)
    {
        var rightChild = node.Right;
        var temp = rightChild.Left;

        // Perform rotation
        rightChild.Left = node;
        node.Right = temp;

        // Update heights (Update child first, then new parent)
        UpdateHeight(node);
        UpdateHeight(rightChild);

        return rightChild;
    }

    private Node RightRotate(Node node)
    {
        var leftChild = node.Left;
        var temp = leftChild.Right;

        // Perform rotation
        leftChild.Right = node;
        node.Left = temp;

        // Update heights
        UpdateHeight(node);
        UpdateHeight(leftChild);

        return leftChild;
    }

    // ===========================
    // CORE LOGIC (RECURSIVE)
    // ===========================

    private Node InsertRecursive(Node node, T key)
    {
        // 1. Standard BST Insertion
        if (node == null)
        {
            _size++; // Only increment size if we actually create a new node
            return new Node(key);
        }

        var comparisonResult = key.CompareTo(node.Key);

        if (comparisonResult < 0)
            node.Left = InsertRecursive(node.Left, key);
        else if (comparisonResult > 0)
            node.Right = InsertRecursive(node.Right, key);
        else
            return node; // Duplicate, do nothing

        // 2. Update Height
        UpdateHeight(node);

        // 3. Rebalance
        var balance = BalanceFactor(node);

        // Left Heavy
        if (balance > 1)
        {
            // Left-Left Case: Inserted key is smaller than left child's key
            if (key.CompareTo(node.Left.Key) < 0)
            {
                return RightRotate(node);
            }
            // Left-Right Case: Inserted key is larger than left child's key
            else if (key.CompareTo(node.Left.Key) > 0)
            {
                node.Left = LeftRotate(node.Left);
                return RightRotate(node);
            }
        }

        // Right Heavy
        if (balance < -1)
        {
            // Right-Right Case: Inserted key is larger than right child's key
            if (key.CompareTo(node.Right.Key) > 0)
            {
                return LeftRotate(node);
            }
            // Right-Left Case: Inserted key is smaller than right child's key
            else if (key.CompareTo(node.Right.Key) < 0)
            {
                node.Right = RightRotate(node.Right);
                return LeftRotate(node);
            }
        }

        return node;
    }

    private Node DeleteRecursive(Node node, T key)
    {
        // 1. Standard BST Delete
        if (node == null) return node;

        var comparisonResult = key.CompareTo(node.Key);

        if (comparisonResult < 0)
        {
            node.Left = DeleteRecursive(node.Left, key);
        }
        else if (comparisonResult > 0)
        {
            node.Right = DeleteRecursive(node.Right, key);
        }
        else
        {
            // Node found. Handle deletion cases:

            // Case 1 & 2: One child or no child
            if ((node.Left == null) || (node.Right == null))
            {
                var temp = node.Left ?? node.Right;

                if (temp == null) // No child
                {
                    temp = node;
                    node = null;
                }
                else // One child
                {
                    node = temp;
                }

                _size--;
            }
            else
            {
                // Case 3: Two children
                var temp = GetMinValueNode(node.Right);
                node.Key = temp.Key;
                node.Right = DeleteRecursive(node.Right, temp.Key);
            }
        }

        if (node == null) return node;

        // 2. Update Height
        UpdateHeight(node);

        // 3. Rebalance
        var balance = BalanceFactor(node);

        // Left Heavy
        if (balance > 1)
        {
            // Check BalanceFactor of Left Child
            if (BalanceFactor(node.Left) >= 0)
            {
                return RightRotate(node);
            }
            else
            {
                node.Left = LeftRotate(node.Left);
                return RightRotate(node);
            }
        }

        // Right Heavy
        if (balance < -1)
        {
            // Check BalanceFactor of Right Child
            if (BalanceFactor(node.Right) <= 0)
            {
                return LeftRotate(node);
            }
            else
            {
                node.Right = RightRotate(node.Right);
                return LeftRotate(node);
            }
        }

        return node;
    }

    private void PrintInOrderRecursive(Node node)
    {
        if (node != null)
        {
            PrintInOrderRecursive(node.Left);
            Console.Write(node.Key + " ");
            PrintInOrderRecursive(node.Right);
        }
    }
}