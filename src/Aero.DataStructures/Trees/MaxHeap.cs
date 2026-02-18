using System;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a MaxHeap, a binary heap where the maximum element is always at the root.
/// </summary>
/// <typeparam name="T">The type of elements in the heap, must be comparable.</typeparam>
public class MaxHeap<T> : BinaryHeap<T> where T : IComparable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxHeap{T}"/> class.
    /// </summary>
    public MaxHeap() : base(HeapType.MaxHeap)
    {
    }

    /// <summary>
    /// Returns the maximum element without removing it.
    /// </summary>
    /// <returns>The maximum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T PeekMax()
    {
        return Peek();
    }

    /// <summary>
    /// Removes and returns the maximum element.
    /// </summary>
    /// <returns>The maximum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T ExtractMax()
    {
        return Extract();
    }
}
