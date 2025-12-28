namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a generic tree data structure.
/// </summary>
/// <typeparam name="T">The type of the values in the tree.</typeparam>
public interface ITree<T>
{
    /// <summary>
    /// Inserts a new value into the tree.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    void Insert(T value);

    /// <summary>
    /// Deletes a value from the tree.
    /// </summary>
    /// <param name="value">The value to delete.</param>
    void Delete(T value);

    /// <summary>
    /// Finds a node with the specified value.
    /// </summary>
    /// <param name="value">The value to find.</param>
    /// <returns>The node if found; otherwise, null.</returns>
    ITreeNode<T> Find(T value);
}