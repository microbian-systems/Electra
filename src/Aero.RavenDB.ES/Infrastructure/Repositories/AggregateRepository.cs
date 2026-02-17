using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Library.Domain;

namespace EventSourcing.Library.Infrastructure.Repositories
{
    /// <summary>
    /// Generic repository implementation for aggregate roots.
    /// Implements the Repository pattern with dependency injection.
    /// Uses the Factory pattern for aggregate instantiation.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    public class AggregateRepository<TAggregate> : IAggregateRepository<TAggregate>
        where TAggregate : IAggregateRoot
    {
        private readonly IEventStore _eventStore;
        private readonly IAggregateFactory<TAggregate> _aggregateFactory;

        public AggregateRepository(
            IEventStore eventStore,
            IAggregateFactory<TAggregate> aggregateFactory)
        {
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _aggregateFactory = aggregateFactory ?? throw new ArgumentNullException(nameof(aggregateFactory));
        }

        public async Task<TAggregate?> GetByIdAsync(
            string aggregateId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
            var eventsList = events.ToList();

            if (!eventsList.Any())
                return default; // Aggregate doesn't exist

            // Use factory to create and hydrate aggregate
            return _aggregateFactory.CreateFromHistory(aggregateId, eventsList);
        }

        public async Task SaveAsync(
            TAggregate aggregate,
            CancellationToken cancellationToken = default)
        {
            if (aggregate == null)
                throw new ArgumentNullException(nameof(aggregate));

            var uncommittedEvents = aggregate.GetUncommittedEvents().ToList();

            if (!uncommittedEvents.Any())
                return; // No changes to persist

            // Calculate expected version (current version minus uncommitted events)
            var expectedVersion = aggregate.Version - uncommittedEvents.Count;

            // Persist events
            await _eventStore.SaveEventsAsync(
                aggregate.Id,
                uncommittedEvents,
                expectedVersion,
                cancellationToken);

            // Mark events as committed
            aggregate.MarkEventsAsCommitted();
        }

        public async Task<bool> ExistsAsync(
            string aggregateId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            return await _eventStore.AggregateExistsAsync(aggregateId, cancellationToken);
        }
    }
}
