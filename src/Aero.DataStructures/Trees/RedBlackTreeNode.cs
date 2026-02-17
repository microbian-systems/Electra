namespace Aero.DataStructures.Trees;

public enum NodeColor
{
    Red,
    Black
}

/// <summary>
/// Represents a node in a Red-Black Tree.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public class RedBlackTreeNode<T> : BinaryTreeNode<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RedBlackTreeNode{T}"/> class.
    /// </summary>
    /// <param name="value">The value of the node.</param>
    public RedBlackTreeNode(T value) : base(value)
    {
        Color = NodeColor.Red; // New nodes are always red
    }

    /// <summary>
    /// Gets or sets the color of the node.
    /// </summary>
    public NodeColor Color { get; set; }
}