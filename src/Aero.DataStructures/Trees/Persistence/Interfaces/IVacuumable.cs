using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Interfaces;

/// <summary>
/// Progress snapshot reported during vacuum operations.
/// </summary>
/// <param name="TotalPages">Total number of pages to process.</param>
/// <param name="ProcessedPages">Number of pages processed so far.</param>
/// <param name="CompactedPages">Number of pages that were actually compacted.</param>
/// <param name="BytesReclaimed">Total bytes reclaimed from tombstones.</param>
public readonly record struct VacuumProgress(
    int TotalPages,
    int ProcessedPages,
    int CompactedPages,
    long BytesReclaimed);

/// <summary>
/// Optional vacuum capability for structures that can fragment.
/// Implemented by B+ tree and BST only.
/// Heaps cannot fragment — do NOT add this to IPriorityTree.
/// </summary>
public interface IVacuumable
{
    /// <summary>
    /// Gets the overall fragmentation ratio across all pages (0.0–1.0).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The fragmentation ratio.</returns>
    ValueTask<double> GetFragmentationAsync(CancellationToken ct = default);

    /// <summary>
    /// Compacts the single most fragmented page that meets the default threshold.
    /// Returns true if a page was compacted, false if nothing needed doing.
    /// Safe to call frequently — low-impact incremental operation.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a page was compacted, false otherwise.</returns>
    ValueTask<bool> VacuumPageAsync(CancellationToken ct = default);

    /// <summary>
    /// Compacts all pages at or above the fragmentation threshold.
    /// Reports progress via IProgress if provided.
    /// Potentially expensive — run during low traffic.
    /// </summary>
    /// <param name="fragmentationThreshold">Minimum fragmentation ratio to vacuum (default 0.5).</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask VacuumAsync(
        double fragmentationThreshold = 0.5,
        IProgress<VacuumProgress>? progress = null,
        CancellationToken ct = default);
}
