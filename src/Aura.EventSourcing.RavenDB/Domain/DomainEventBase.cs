using System;
using System.Collections.Generic;

namespace EventSourcing.Library.Domain
{
    /// <summary>
    /// Abstract base class for domain events.
    /// Implements Template Method pattern for common event behavior.
    /// </summary>
    public abstract class DomainEventBase : IDomainEvent
    {
        protected DomainEventBase()
        {
            EventId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            EventType = GetType().AssemblyQualifiedName 
                ?? throw new InvalidOperationException("Unable to determine event type");
            Metadata = new Dictionary<string, object>();
        }

        public Guid EventId { get; set; }
        public string AggregateId { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public IDictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Adds metadata to the event for cross-cutting concerns
        /// </summary>
        public void AddMetadata(string key, object value)
        {
            Metadata ??= new Dictionary<string, object>();
            Metadata[key] = value;
        }

        /// <summary>
        /// Template method for validation - can be overridden by derived classes
        /// </summary>
        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(AggregateId))
                throw new InvalidOperationException("AggregateId cannot be null or empty");
            
            if (Version <= 0)
                throw new InvalidOperationException("Version must be greater than 0");
        }
    }
}
