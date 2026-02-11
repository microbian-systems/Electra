using System;

namespace EventSourcing.Library.Infrastructure.Snapshots
{
    /// <summary>
    /// Represents a point-in-time snapshot of an aggregate's state.
    /// Used for performance optimization to avoid replaying all events.
    /// Follows the Memento pattern from Gang of Four.
    /// </summary>
    public interface ISnapshot
    {
        /// <summary>
        /// The aggregate identifier
        /// </summary>
        string AggregateId { get; }

        /// <summary>
        /// The version of the aggregate when this snapshot was taken
        /// </summary>
        int Version { get; }

        /// <summary>
        /// When this snapshot was created
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Serialized state data
        /// </summary>
        string StateData { get; }

        /// <summary>
        /// Type of the aggregate (for deserialization)
        /// </summary>
        string AggregateType { get; }
    }

    /// <summary>
    /// Concrete implementation of snapshot
    /// </summary>
    public class Snapshot : ISnapshot
    {
        public string AggregateId { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateTime Timestamp { get; set; }
        public string StateData { get; set; } = string.Empty;
        public string AggregateType { get; set; } = string.Empty;
    }
}
