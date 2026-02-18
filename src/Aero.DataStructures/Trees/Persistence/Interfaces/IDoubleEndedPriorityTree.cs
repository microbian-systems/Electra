using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Interfaces;

/// <summary>
/// Interface for double-ended priority tree structures that support access to both
/// minimum and maximum elements efficiently.
/// </summary>
/// <typeparam name="T">The type of elements.</typeparam>
public interface IDoubleEndedPriorityTree<T> : IPriorityTree<T>
{
    /// <summary>
    /// Returns the maximum element without removing it.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The maximum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tree is empty.</exception>
    ValueTask<T> PeekMaxAsync(CancellationToken ct = default);

    /// <summary>
    /// Removes and returns the maximum element.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The extracted maximum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the tree is empty.</exception>
    ValueTask<T> ExtractMaxAsync(CancellationToken ct = default);
}
