namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a node in a Splay Tree.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public class SplayTreeNode<T> : BinaryTreeNode<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SplayTreeNode{T}"/> class.
    /// </summary>
    /// <param name="value">The value of the node.</param>
    public SplayTreeNode(T value) : base(value)
    {
    }
}