using System.Collections.Generic;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a node in a B-Tree.
/// </summary>
/// <typeparam name="T">The type of the keys in the B-Tree.</typeparam>
public class BTreeNode<T>
{
    public BTreeNode(int degree)
    {
        Keys = new List<T>(degree - 1);
        Children = new List<BTreeNode<T>>(degree);
    }

    public List<T> Keys { get; }
    public List<BTreeNode<T>> Children { get; }
    public bool IsLeaf => Children.Count == 0;
}