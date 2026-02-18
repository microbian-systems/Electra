namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// Lightweight per-page occupancy metadata tracked by the backend.
/// Does not require reading the full page contents.
/// </summary>
/// <param name="PageId">The stable ID of the page.</param>
/// <param name="TotalSlots">Total number of record slots in the page.</param>
/// <param name="LiveSlots">Number of non-deleted records.</param>
/// <param name="DeadSlots">Number of tombstoned/deleted records.</param>
/// <param name="IsFree">True if the page is on the free list.</param>
public readonly record struct PageMetadata(
    long PageId,
    int TotalSlots,
    int LiveSlots,
    int DeadSlots,
    bool IsFree)
{
    /// <summary>
    /// Ratio of dead slots to total slots. 0.0 = no fragmentation.
    /// </summary>
    public double Fragmentation => TotalSlots == 0 ? 0.0 : (double)DeadSlots / TotalSlots;
}
