using System.Collections.Generic;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a binary tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree.</typeparam>
public class BinaryTree<T> : ITree<T>
{
    /// <summary>
    /// Gets or sets the root of the tree.
    /// </summary>
    public BinaryTreeNode<T> Root { get; set; }

    /// <inheritdoc />
    public void Insert(T value)
    {
        if (Root == null)
        {
            Root = new BinaryTreeNode<T>(value);
            return;
        }

        var queue = new Queue<BinaryTreeNode<T>>();
        queue.Enqueue(Root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Left == null)
            {
                current.Left = new BinaryTreeNode<T>(value);
                return;
            }
                
            queue.Enqueue(current.Left);

            if (current.Right == null)
            {
                current.Right = new BinaryTreeNode<T>(value);
                return;
            }
                
            queue.Enqueue(current.Right);
        }
    }

    /// <inheritdoc />
    public void Delete(T value)
    {
        // Deletion in a simple binary tree is complex and can be ambiguous.
        // A common approach is to find the node, replace it with the deepest, rightmost node,
        // and then delete the deepest, rightmost node.
        // This is a simplified implementation. For a more robust solution,
        // a more specific tree type (like BST) is recommended.
        if (Root == null) return;

        if (Root.Value.Equals(value))
        {
            Root = null;
            return;
        }

        var queue = new Queue<BinaryTreeNode<T>>();
        queue.Enqueue(Root);
        BinaryTreeNode<T> nodeToDelete = null;
        BinaryTreeNode<T> deepestNode = null;

        while (queue.Count > 0)
        {
            deepestNode = queue.Dequeue();
            if (deepestNode.Value.Equals(value))
            {
                nodeToDelete = deepestNode;
            }

            if (deepestNode.Left != null)
            {
                queue.Enqueue(deepestNode.Left);
            }

            if (deepestNode.Right != null)
            {
                queue.Enqueue(deepestNode.Right);
            }
        }

        if (nodeToDelete != null)
        {
            // Replace the node to delete's value with the deepest node's value
            nodeToDelete.Value = deepestNode.Value;
            // Delete the deepest node
            DeleteDeepest(Root, deepestNode);
        }
    }
        
    private void DeleteDeepest(BinaryTreeNode<T> root, BinaryTreeNode<T> delNode)
    {
        var q = new Queue<BinaryTreeNode<T>>();
        q.Enqueue(root);
 
        // Do level order traversal until last node
        BinaryTreeNode<T> temp = null;
        while (q.Count > 0)
        {
            temp = q.Dequeue();
            if (temp == delNode)
            {
                temp = null;
                return;
            }
            if (temp.Right!=null)
            {
                if (temp.Right == delNode)
                {
                    temp.Right = null;
                    return;
                }
                else
                    q.Enqueue(temp.Right);
            }
 
            if (temp.Left != null)
            {
                if (temp.Left == delNode)
                {
                    temp.Left = null;
                    return;
                }
                else
                    q.Enqueue(temp.Left);
            }
        }
    }


    /// <inheritdoc />
    public ITreeNode<T> Find(T value)
    {
        return Find(Root, value);
    }

    private ITreeNode<T> Find(BinaryTreeNode<T> node, T value)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Value.Equals(value))
        {
            return node;
        }

        var foundNode = Find(node.Left, value);
        if (foundNode == null)
        {
            foundNode = Find(node.Right, value);
        }

        return foundNode;
    }
}