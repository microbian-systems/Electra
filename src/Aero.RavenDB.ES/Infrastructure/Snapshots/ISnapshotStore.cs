using System.Threading;
using System.Threading.Tasks;

namespace EventSourcing.Library.Infrastructure.Snapshots
{
    /// <summary>
    /// Interface for snapshot storage and retrieval.
    /// Follows the Repository pattern for snapshot persistence.
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// Saves a snapshot for an aggregate
        /// </summary>
        Task SaveSnapshotAsync(
            ISnapshot snapshot,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the latest snapshot for an aggregate
        /// </summary>
        Task<ISnapshot?> GetLatestSnapshotAsync(
            string aggregateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes old snapshots, keeping only the most recent N snapshots
        /// </summary>
        Task CleanupSnapshotsAsync(
            string aggregateId,
            int keepCount = 3,
            CancellationToken cancellationToken = default);
    }
}
