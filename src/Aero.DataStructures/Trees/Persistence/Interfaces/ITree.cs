using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Interfaces;

/// <summary>
/// Base interface for all persistent tree data structures.
/// </summary>
/// <typeparam name="T">The type of elements stored in the tree.</typeparam>
public interface ITree<T>
{
    /// <summary>
    /// Inserts a value into the tree.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask InsertAsync(T value, CancellationToken ct = default);

    /// <summary>
    /// Checks if the tree contains the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the value exists in the tree, otherwise false.</returns>
    ValueTask<bool> ContainsAsync(T value, CancellationToken ct = default);

    /// <summary>
    /// Gets the number of elements in the tree.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of elements.</returns>
    ValueTask<long> CountAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears all elements from the tree.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    ValueTask ClearAsync(CancellationToken ct = default);
}
