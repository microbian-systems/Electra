using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Interfaces;

/// <summary>
/// Interface for priority-based tree structures (heaps).
/// This interface should NOT be implemented by BST or B+ tree types.
/// </summary>
/// <typeparam name="T">The type of elements.</typeparam>
public interface IPriorityTree<T> : ITree<T>
{
    /// <summary>
    /// Returns the top-priority element without removing it.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The top-priority element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tree is empty.</exception>
    ValueTask<T> PeekAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes and returns the top-priority element.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The extracted element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tree is empty.</exception>
    ValueTask<T> ExtractAsync(CancellationToken ct = default);
}
