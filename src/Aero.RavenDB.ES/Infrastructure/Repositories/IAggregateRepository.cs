using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Library.Domain;

namespace EventSourcing.Library.Infrastructure.Repositories
{
    /// <summary>
    /// Generic repository interface for aggregate roots.
    /// Follows the Repository pattern from DDD.
    /// Type parameter is constrained to IAggregateRoot for type safety.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    public interface IAggregateRepository<TAggregate> where TAggregate : IAggregateRoot
    {
        /// <summary>
        /// Retrieves an aggregate by its ID, reconstructing it from events
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The aggregate instance, or null if not found</returns>
        Task<TAggregate?> GetByIdAsync(
            string aggregateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves an aggregate by persisting its uncommitted events
        /// </summary>
        /// <param name="aggregate">The aggregate to save</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task SaveAsync(
            TAggregate aggregate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an aggregate exists
        /// </summary>
        Task<bool> ExistsAsync(
            string aggregateId,
            CancellationToken cancellationToken = default);
    }
}
