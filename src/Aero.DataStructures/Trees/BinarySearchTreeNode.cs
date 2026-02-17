namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a node in a Binary Search Tree.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public class BinarySearchTreeNode<T> : BinaryTreeNode<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BinarySearchTreeNode{T}"/> class.
    /// </summary>
    /// <param name="value">The value of the node.</param>
    public BinarySearchTreeNode(T value) : base(value)
    {
    }
}