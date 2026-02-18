namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a node in an AVL Tree.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public class AvlTreeNode<T> : BinaryTreeNode<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AvlTreeNode{T}"/> class.
    /// </summary>
    /// <param name="value">The value of the node.</param>
    public AvlTreeNode(T value) : base(value)
    {
        Height = 1;
    }

    /// <summary>
    /// Gets or sets the height of the node.
    /// </summary>
    public int Height { get; set; }
}