namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a node in a Treap.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public class TreapNode<T> : BinaryTreeNode<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TreapNode{T}"/> class.
    /// </summary>
    /// <param name="value">The value of the node.</param>
    /// <param name="priority">The priority of the node.</param>
    public TreapNode(T value, int priority) : base(value)
    {
        Priority = priority;
    }

    /// <summary>
    /// Gets or sets the priority of the node.
    /// </summary>
    public int Priority { get; set; }
}