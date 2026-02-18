namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a generic heap data structure.
/// </summary>
/// <typeparam name="T">The type of elements in the heap.</typeparam>
public interface IHeap<T>
{
    /// <summary>
    /// Gets the number of elements in the heap.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Inserts an item into the heap.
    /// </summary>
    /// <param name="item">The item to insert.</param>
    void Insert(T item);

    /// <summary>
    /// Returns the minimum or maximum element without removing it.
    /// </summary>
    /// <returns>The top element.</returns>
    T Peek();

    /// <summary>
    /// Removes and returns the minimum or maximum element.
    /// </summary>
    /// <returns>The extracted element.</returns>
    T Extract();
}
