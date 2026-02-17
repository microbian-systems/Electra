using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a node in a binary tree.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public class BinaryTreeNode<T> : ITreeNode<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryTreeNode{T}"/> class.
    /// </summary>
    /// <param name="value">The value of the node.</param>
    public BinaryTreeNode(T value)
    {
        Value = value;
    }

    /// <inheritdoc />
    public T Value { get; set; }

    /// <summary>
    /// Gets or sets the left child of the node.
    /// </summary>
    public BinaryTreeNode<T> Left { get; set; }

    /// <summary>
    /// Gets or sets the right child of the node.
    /// </summary>
    public BinaryTreeNode<T> Right { get; set; }

    /// <inheritdoc />
    public IEnumerable<ITreeNode<T>> Children
    {
        get
        {
            if (Left != null)
            {
                yield return Left;
            }

            if (Right != null)
            {
                yield return Right;
            }
        }
    }
}