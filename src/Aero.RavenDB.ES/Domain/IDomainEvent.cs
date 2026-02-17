using System;

namespace EventSourcing.Library.Domain
{
    /// <summary>
    /// Base interface for all domain events.
    /// Follows the Interface Segregation Principle - minimal, focused contract.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Unique identifier for this specific event instance
        /// </summary>
        Guid EventId { get; }
        
        /// <summary>
        /// Identifier of the aggregate this event belongs to
        /// </summary>
        string AggregateId { get; }
        
        /// <summary>
        /// Version of the aggregate when this event was created
        /// Used for optimistic concurrency control
        /// </summary>
        int Version { get; }
        
        /// <summary>
        /// UTC timestamp when the event occurred
        /// </summary>
        DateTime Timestamp { get; }
        
        /// <summary>
        /// Fully qualified type name of the event
        /// Used for polymorphic deserialization
        /// </summary>
        string EventType { get; }
        
        /// <summary>
        /// Optional metadata for cross-cutting concerns (correlation, causation, user context)
        /// </summary>
        IDictionary<string, object>? Metadata { get; }
    }
}
