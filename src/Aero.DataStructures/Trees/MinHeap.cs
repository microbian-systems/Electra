using System;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a MinHeap, a binary heap where the minimum element is always at the root.
/// </summary>
/// <typeparam name="T">The type of elements in the heap, must be comparable.</typeparam>
public class MinHeap<T> : BinaryHeap<T> where T : IComparable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinHeap{T}"/> class.
    /// </summary>
    public MinHeap() : base(HeapType.MinHeap)
    {
    }

    /// <summary>
    /// Returns the minimum element without removing it.
    /// </summary>
    /// <returns>The minimum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T PeekMin()
    {
        return Peek();
    }

    /// <summary>
    /// Removes and returns the minimum element.
    /// </summary>
    /// <returns>The minimum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T ExtractMin()
    {
        return Extract();
    }
}
