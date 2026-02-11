using System;
using System.Linq;
using Raven.Client.Documents.Indexes;

namespace EventSourcing.RavenDB.Infrastructure.Persistence.Indexes
{
    /// <summary>
    /// Index for efficiently querying events by aggregate ID and version.
    /// RavenDB uses Map-Reduce indexes instead of traditional database indexes.
    /// This is critical for optimistic concurrency control and event retrieval.
    /// </summary>
    public class Events_ByAggregateIdAndVersion : AbstractIndexCreationTask<EventDocument>
    {
        public Events_ByAggregateIdAndVersion()
        {
            // Map function - defines what to index
            Map = events => from evt in events
                           select new
                           {
                               evt.AggregateId,
                               evt.Version,
                               evt.Timestamp
                           };

            // Store fields for faster retrieval
            Store(x => x.AggregateId, FieldStorage.Yes);
            Store(x => x.Version, FieldStorage.Yes);
            Store(x => x.Timestamp, FieldStorage.Yes);

            // Index for sorting
            Index(x => x.Version, FieldIndexing.Default);
            Index(x => x.Timestamp, FieldIndexing.Default);
        }
    }

    /// <summary>
    /// Index for querying events by timestamp.
    /// Useful for temporal queries and debugging.
    /// </summary>
    public class Events_ByTimestamp : AbstractIndexCreationTask<EventDocument>
    {
        public Events_ByTimestamp()
        {
            Map = events => from evt in events
                           select new
                           {
                               evt.Timestamp,
                               evt.AggregateId,
                               evt.EventType
                           };

            Store(x => x.Timestamp, FieldStorage.Yes);
            Index(x => x.Timestamp, FieldIndexing.Default);
        }
    }

    /// <summary>
    /// Index for querying events by type.
    /// Useful for building projections and event handlers.
    /// </summary>
    public class Events_ByEventType : AbstractIndexCreationTask<EventDocument>
    {
        public Events_ByEventType()
        {
            Map = events => from evt in events
                           select new
                           {
                               evt.EventType,
                               evt.Timestamp,
                               evt.AggregateId
                           };

            Store(x => x.EventType, FieldStorage.Yes);
            Index(x => x.EventType, FieldIndexing.Exact);
        }
    }

    /// <summary>
    /// Index for getting the current version of an aggregate.
    /// Uses Map-Reduce to efficiently get the max version per aggregate.
    /// </summary>
    public class Events_CurrentVersionByAggregate : AbstractIndexCreationTask<EventDocument, Events_CurrentVersionByAggregate.Result>
    {
        public class Result
        {
            public string AggregateId { get; set; } = string.Empty;
            public int CurrentVersion { get; set; }
            public int EventCount { get; set; }
            public DateTime? LastEventTimestamp { get; set; }
        }

        public Events_CurrentVersionByAggregate()
        {
            // Map phase - extract data from each event
            Map = events => from evt in events
                           select new Result
                           {
                               AggregateId = evt.AggregateId,
                               CurrentVersion = evt.Version,
                               EventCount = 1,
                               LastEventTimestamp = evt.Timestamp
                           };

            // Reduce phase - aggregate by AggregateId
            Reduce = results => from result in results
                               group result by result.AggregateId into g
                               select new Result
                               {
                                   AggregateId = g.Key,
                                   CurrentVersion = g.Max(x => x.CurrentVersion),
                                   EventCount = g.Sum(x => x.EventCount),
                                   LastEventTimestamp = g.Max(x => x.LastEventTimestamp)
                               };

            Store(x => x.AggregateId, FieldStorage.Yes);
            Store(x => x.CurrentVersion, FieldStorage.Yes);
            Store(x => x.EventCount, FieldStorage.Yes);
            Store(x => x.LastEventTimestamp, FieldStorage.Yes);
        }
    }
}
