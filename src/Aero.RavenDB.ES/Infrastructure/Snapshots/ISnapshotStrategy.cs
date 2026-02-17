using EventSourcing.Library.Domain;

namespace EventSourcing.Library.Infrastructure.Snapshots
{
    /// <summary>
    /// Strategy interface for determining when to take snapshots.
    /// Follows the Strategy pattern to allow different snapshotting policies.
    /// </summary>
    public interface ISnapshotStrategy
    {
        /// <summary>
        /// Determines if a snapshot should be taken for the given aggregate
        /// </summary>
        bool ShouldTakeSnapshot(IAggregateRoot aggregate);
    }

    /// <summary>
    /// Takes a snapshot every N events
    /// </summary>
    public class EventCountSnapshotStrategy : ISnapshotStrategy
    {
        private readonly int _eventThreshold;

        public EventCountSnapshotStrategy(int eventThreshold = 100)
        {
            _eventThreshold = eventThreshold;
        }

        public bool ShouldTakeSnapshot(IAggregateRoot aggregate)
        {
            return aggregate.Version > 0 && aggregate.Version % _eventThreshold == 0;
        }
    }

    /// <summary>
    /// Never takes snapshots
    /// </summary>
    public class NoSnapshotStrategy : ISnapshotStrategy
    {
        public bool ShouldTakeSnapshot(IAggregateRoot aggregate) => false;
    }

    /// <summary>
    /// Always takes snapshots (useful for testing or high-performance scenarios)
    /// </summary>
    public class AlwaysSnapshotStrategy : ISnapshotStrategy
    {
        public bool ShouldTakeSnapshot(IAggregateRoot aggregate) => true;
    }
}
