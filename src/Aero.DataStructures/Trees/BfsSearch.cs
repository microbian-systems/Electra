using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Performs a Breadth-First Search (Level-Order) on a binary tree.
/// </summary>
/// <typeparam name="T">The type of the values in the tree.</typeparam>
public class BfsSearch<T> : TreeSearch<T>
{
    public BfsSearch(ITree<T> tree) : base(tree)
    {
    }

    public override IEnumerable<ITreeNode<T>> Search()
    {
        var binaryTree = Tree as BinaryTree<T>;
        if (binaryTree?.Root == null)
        {
            yield break;
        }

        var queue = new Queue<BinaryTreeNode<T>>();
        queue.Enqueue(binaryTree.Root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            if (current.Left != null)
            {
                queue.Enqueue((BinaryTreeNode<T>)current.Left);
            }

            if (current.Right != null)
            {
                queue.Enqueue((BinaryTreeNode<T>)current.Right);
            }
        }
    }
}