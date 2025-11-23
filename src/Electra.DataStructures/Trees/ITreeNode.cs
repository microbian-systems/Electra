using System.Collections.Generic;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a node in a tree.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public interface ITreeNode<T>
{
    /// <summary>
    /// Gets or sets the value of the node.
    /// </summary>
    T Value { get; set; }

    /// <summary>
    /// Gets the children of the node.
    /// </summary>
    IEnumerable<ITreeNode<T>> Children { get; }
}