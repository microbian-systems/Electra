using System.Collections.Generic;
using EventSourcing.Library.Domain;

namespace EventSourcing.Library.Infrastructure.Serialization
{
    /// <summary>
    /// Interface for event serialization/deserialization.
    /// Follows the Strategy pattern to allow different serialization implementations.
    /// </summary>
    public interface IEventSerializer
    {
        /// <summary>
        /// Serializes a domain event to a string representation
        /// </summary>
        string Serialize(IDomainEvent domainEvent);

        /// <summary>
        /// Deserializes a string to a domain event
        /// </summary>
        IDomainEvent Deserialize(string eventData, string eventType);

        /// <summary>
        /// Serializes event metadata
        /// </summary>
        string? SerializeMetadata(IDictionary<string, object>? metadata);

        /// <summary>
        /// Deserializes event metadata
        /// </summary>
        IDictionary<string, object>? DeserializeMetadata(string? metadata);
    }
}
