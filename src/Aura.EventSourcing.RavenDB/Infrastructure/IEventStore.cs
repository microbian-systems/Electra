using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.RavenDB.Domain;

namespace EventSourcing.RavenDB.Infrastructure
{
    /// <summary>
    /// Interface for the event store.
    /// Follows the Repository pattern for event persistence.
    /// This interface is identical to the EF Core version - demonstrating 
    /// that the domain layer is independent of the persistence mechanism.
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Saves events for an aggregate with optimistic concurrency control.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier</param>
        /// <param name="events">Events to persist</param>
        /// <param name="expectedVersion">Expected current version for concurrency check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <exception cref="ConcurrencyException">Thrown when version conflict detected</exception>
        Task SaveEventsAsync(
            string aggregateId, 
            IEnumerable<IDomainEvent> events, 
            int expectedVersion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all events for an aggregate
        /// </summary>
        Task<IEnumerable<IDomainEvent>> GetEventsAsync(
            string aggregateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves events for an aggregate starting from a specific version.
        /// Useful for snapshot optimization.
        /// </summary>
        Task<IEnumerable<IDomainEvent>> GetEventsAsync(
            string aggregateId, 
            int fromVersion,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if an aggregate exists by looking for any events
        /// </summary>
        Task<bool> AggregateExistsAsync(
            string aggregateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current version of an aggregate
        /// </summary>
        Task<int> GetAggregateVersionAsync(
            string aggregateId,
            CancellationToken cancellationToken = default);
    }
}
