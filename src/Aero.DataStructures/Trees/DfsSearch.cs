using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

public enum DfsTraversalOrder
{
    Preorder,
    Inorder,
    Postorder
}

/// <summary>
/// Performs a Depth-First Search on a binary tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree.</typeparam>
public class DfsSearch<T> : TreeSearch<T>
{
    private readonly DfsTraversalOrder _traversalOrder;

    public DfsSearch(ITree<T> tree, DfsTraversalOrder traversalOrder) : base(tree)
    {
        _traversalOrder = traversalOrder;
    }

    public override IEnumerable<ITreeNode<T>> Search()
    {
        var binaryTree = Tree as BinaryTree<T>;
        if (binaryTree == null)
        {
            // Or handle other tree types if necessary
            yield break;
        }

        switch (_traversalOrder)
        {
            case DfsTraversalOrder.Preorder:
                foreach (var node in Preorder(binaryTree.Root))
                {
                    yield return node;
                }
                break;
            case DfsTraversalOrder.Inorder:
                foreach (var node in Inorder(binaryTree.Root))
                {
                    yield return node;
                }
                break;
            case DfsTraversalOrder.Postorder:
                foreach (var node in Postorder(binaryTree.Root))
                {
                    yield return node;
                }
                break;
        }
    }

    private IEnumerable<ITreeNode<T>> Preorder(BinaryTreeNode<T> node)
    {
        if (node == null) yield break;
            
        yield return node;
            
        foreach (var child in Preorder((BinaryTreeNode<T>)node.Left))
        {
            yield return child;
        }
        foreach (var child in Preorder((BinaryTreeNode<T>)node.Right))
        {
            yield return child;
        }
    }

    private IEnumerable<ITreeNode<T>> Inorder(BinaryTreeNode<T> node)
    {
        if (node == null) yield break;

        foreach (var child in Inorder((BinaryTreeNode<T>)node.Left))
        {
            yield return child;
        }
            
        yield return node;
            
        foreach (var child in Inorder((BinaryTreeNode<T>)node.Right))
        {
            yield return child;
        }
    }

    private IEnumerable<ITreeNode<T>> Postorder(BinaryTreeNode<T> node)
    {
        if (node == null) yield break;

        foreach (var child in Postorder((BinaryTreeNode<T>)node.Left))
        {
            yield return child;
        }
            
        foreach (var child in Postorder((BinaryTreeNode<T>)node.Right))
        {
            yield return child;
        }
            
        yield return node;
    }
}