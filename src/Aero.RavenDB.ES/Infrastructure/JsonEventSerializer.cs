using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventSourcing.Library.Domain;

namespace EventSourcing.Library.Infrastructure.Serialization
{
    /// <summary>
    /// JSON-based event serializer using System.Text.Json.
    /// Implements the Strategy pattern for serialization.
    /// </summary>
    public class JsonEventSerializer : IEventSerializer
    {
        private readonly JsonSerializerOptions _options;

        public JsonEventSerializer()
        {
            _options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }

        public JsonEventSerializer(JsonSerializerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string Serialize(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _options);
        }

        public IDomainEvent Deserialize(string eventData, string eventType)
        {
            if (string.IsNullOrWhiteSpace(eventData))
                throw new ArgumentException("Event data cannot be null or empty", nameof(eventData));

            if (string.IsNullOrWhiteSpace(eventType))
                throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));

            var type = Type.GetType(eventType);
            if (type == null)
                throw new InvalidOperationException($"Unable to resolve event type: {eventType}");

            var @event = JsonSerializer.Deserialize(eventData, type, _options) as IDomainEvent;
            if (@event == null)
                throw new InvalidOperationException($"Failed to deserialize event of type: {eventType}");

            return @event;
        }

        public string? SerializeMetadata(IDictionary<string, object>? metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return null;

            return JsonSerializer.Serialize(metadata, _options);
        }

        public IDictionary<string, object>? DeserializeMetadata(string? metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
                return null;

            return JsonSerializer.Deserialize<Dictionary<string, object>>(metadata, _options);
        }
    }
}
