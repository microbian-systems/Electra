using System.Collections.Generic;

namespace EventSourcing.Library.Domain
{
    /// <summary>
    /// Interface for aggregate roots in Event Sourcing.
    /// Follows the Aggregate pattern from Domain-Driven Design.
    /// </summary>
    public interface IAggregateRoot
    {
        /// <summary>
        /// Unique identifier for this aggregate
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Current version of the aggregate
        /// Increments with each event applied
        /// </summary>
        int Version { get; }
        
        /// <summary>
        /// Gets all uncommitted events that have been raised but not yet persisted
        /// </summary>
        IReadOnlyCollection<IDomainEvent> GetUncommittedEvents();
        
        /// <summary>
        /// Marks all uncommitted events as committed after successful persistence
        /// </summary>
        void MarkEventsAsCommitted();
        
        /// <summary>
        /// Loads the aggregate state from a historical event stream
        /// </summary>
        void LoadFromHistory(IEnumerable<IDomainEvent> history);
    }
}
