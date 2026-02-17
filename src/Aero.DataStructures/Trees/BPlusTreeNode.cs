using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a node in a B+ Tree.
/// </summary>
/// <typeparam name="T">The type of the keys in the B+ Tree.</typeparam>
public class BPlusTreeNode<T>
{
    public BPlusTreeNode(int degree)
    {
        Keys = new List<T>(degree);
        Children = new List<BPlusTreeNode<T>>(degree + 1);
    }

    public List<T> Keys { get; }
    public List<BPlusTreeNode<T>> Children { get; }
    public bool IsLeaf { get; set; }
    public BPlusTreeNode<T> Next { get; set; } // For leaf nodes
}