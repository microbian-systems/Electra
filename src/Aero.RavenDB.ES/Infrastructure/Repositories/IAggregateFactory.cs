using System.Collections.Generic;
using EventSourcing.Library.Domain;

namespace EventSourcing.Library.Infrastructure.Repositories
{
    /// <summary>
    /// Factory interface for creating aggregate instances.
    /// Follows the Abstract Factory pattern.
    /// Necessary because aggregates need to be reconstructed from events.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    public interface IAggregateFactory<out TAggregate> where TAggregate : IAggregateRoot
    {
        /// <summary>
        /// Creates an aggregate instance from a historical event stream
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier</param>
        /// <param name="events">Historical events to replay</param>
        /// <returns>Hydrated aggregate instance</returns>
        TAggregate CreateFromHistory(string aggregateId, IEnumerable<IDomainEvent> events);
    }
}
