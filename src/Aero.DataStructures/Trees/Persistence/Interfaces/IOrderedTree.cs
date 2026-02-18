using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Interfaces;

/// <summary>
/// Interface for ordered tree structures that support range queries and ordered traversal.
/// This interface should NOT be implemented by heap types.
/// </summary>
/// <typeparam name="T">The type of elements, must be comparable.</typeparam>
public interface IOrderedTree<T> : ITree<T> where T : IComparable<T>
{
    /// <summary>
    /// Gets the minimum value in the tree.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The minimum value, or null if the tree is empty.</returns>
    ValueTask<T?> MinAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the maximum value in the tree.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The maximum value, or null if the tree is empty.</returns>
    ValueTask<T?> MaxAsync(CancellationToken ct = default);

    /// <summary>
    /// Enumerates all values in ascending order.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of values in ascending order.</returns>
    IAsyncEnumerable<T> InOrderAsync(CancellationToken ct = default);

    /// <summary>
    /// Scans values within a specified range [from, to] inclusive.
    /// </summary>
    /// <param name="from">The start of the range (inclusive).</param>
    /// <param name="to">The end of the range (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async enumerable of values in the range.</returns>
    IAsyncEnumerable<T> ScanAsync(T from, T to, CancellationToken ct = default);
}
