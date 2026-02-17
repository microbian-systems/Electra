using System;

namespace EventSourcing.RavenDB.Infrastructure.Persistence
{
    /// <summary>
    /// RavenDB document representing a persisted domain event.
    /// RavenDB uses documents instead of relational tables.
    /// Follows the Data Transfer Object pattern for document database mapping.
    /// </summary>
    public class EventDocument
    {
        /// <summary>
        /// RavenDB document ID format: events/{guid}
        /// RavenDB uses string IDs with collection prefixes
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Unique identifier for this event record
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Identifier of the aggregate this event belongs to
        /// </summary>
        public string AggregateId { get; set; } = string.Empty;

        /// <summary>
        /// Fully qualified type name of the event for deserialization
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// JSON-serialized event data
        /// RavenDB stores this as a nested JSON object
        /// </summary>
        public string EventData { get; set; } = string.Empty;

        /// <summary>
        /// JSON-serialized metadata
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Version of the aggregate when this event was created
        /// Used for optimistic concurrency control
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// UTC timestamp when the event occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// UTC timestamp when the event was persisted to the database
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// RavenDB metadata tag for easier querying
        /// Collection name for this document type
        /// </summary>
        public string Collection => "Events";
    }
}
