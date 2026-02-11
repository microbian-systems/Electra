using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.RavenDB.Domain;
using EventSourcing.RavenDB.Infrastructure.Persistence;
using EventSourcing.RavenDB.Infrastructure.Persistence.Indexes;
using EventSourcing.RavenDB.Infrastructure.Serialization;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

namespace EventSourcing.RavenDB.Infrastructure
{
    /// <summary>
    /// RavenDB implementation of the event store.
    /// Follows the Repository pattern with Unit of Work (IDocumentSession).
    /// Key differences from EF Core version:
    /// - Uses IDocumentStore and IDocumentSession instead of DbContext
    /// - Leverages RavenDB's optimistic concurrency with ETags
    /// - Uses RavenDB indexes for efficient querying
    /// - Document-based storage instead of relational tables
    /// </summary>
    public class RavenDbEventStore : IEventStore
    {
        private readonly IDocumentStore _documentStore;
        private readonly IEventSerializer _serializer;

        public RavenDbEventStore(
            IDocumentStore documentStore,
            IEventSerializer serializer)
        {
            _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task SaveEventsAsync(
            string aggregateId,
            IEnumerable<IDomainEvent> events,
            int expectedVersion,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            if (events == null)
                throw new ArgumentNullException(nameof(events));

            var eventsList = events.ToList();
            if (!eventsList.Any())
                return; // Nothing to save

            // RavenDB uses session-based Unit of Work pattern
            using var session = _documentStore.OpenAsyncSession();
            
            // Configure session for optimistic concurrency
            session.Advanced.UseOptimisticConcurrency = true;

            try
            {
                // Check current version for optimistic concurrency
                var currentVersion = await GetAggregateVersionInternalAsync(session, aggregateId, cancellationToken);

                if (currentVersion != expectedVersion)
                {
                    throw new ConcurrencyException(aggregateId, expectedVersion, currentVersion);
                }

                // Check for duplicate versions (extra safety)
                // In RavenDB, we need to explicitly check since there's no unique constraint
                var existingVersions = await session.Query<EventDocument, Events_ByAggregateIdAndVersion>()
                    .Where(e => e.AggregateId == aggregateId)
                    .Select(e => e.Version)
                    .ToListAsync(cancellationToken);

                var now = DateTime.UtcNow;
                foreach (var domainEvent in eventsList)
                {
                    // Verify no duplicate version exists
                    if (existingVersions.Contains(domainEvent.Version))
                    {
                        throw new ConcurrencyException(
                            $"Event with version {domainEvent.Version} already exists for aggregate {aggregateId}");
                    }

                    var eventDocument = new EventDocument
                    {
                        EventId = domainEvent.EventId,
                        AggregateId = aggregateId,
                        EventType = domainEvent.EventType,
                        EventData = _serializer.Serialize(domainEvent),
                        Metadata = _serializer.SerializeMetadata(domainEvent.Metadata),
                        Version = domainEvent.Version,
                        Timestamp = domainEvent.Timestamp,
                        CreatedAt = now
                    };

                    // RavenDB will auto-generate the Id
                    await session.StoreAsync(eventDocument, cancellationToken);
                }

                // SaveChanges commits the unit of work
                await session.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyException)
            {
                // Re-throw our domain exception
                throw;
            }
            catch (Raven.Client.Exceptions.ConcurrencyException ex)
            {
                // RavenDB optimistic concurrency violation
                var currentVersion = await GetAggregateVersionAsync(aggregateId, cancellationToken);
                throw new ConcurrencyException(aggregateId, expectedVersion, currentVersion, ex);
            }
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(
            string aggregateId,
            CancellationToken cancellationToken = default)
        {
            return await GetEventsAsync(aggregateId, fromVersion: 0, cancellationToken);
        }

        public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(
            string aggregateId,
            int fromVersion,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            using var session = _documentStore.OpenAsyncSession();

            // Query using the index for efficiency
            var eventDocuments = await session.Query<EventDocument, Events_ByAggregateIdAndVersion>()
                .Where(e => e.AggregateId == aggregateId && e.Version >= fromVersion)
                .OrderBy(e => e.Version)
                .ToListAsync(cancellationToken);

            var events = new List<IDomainEvent>();
            foreach (var document in eventDocuments)
            {
                var domainEvent = _serializer.Deserialize(document.EventData, document.EventType);
                
                // Restore metadata if present
                if (!string.IsNullOrWhiteSpace(document.Metadata))
                {
                    domainEvent.Metadata = _serializer.DeserializeMetadata(document.Metadata);
                }

                events.Add(domainEvent);
            }

            return events;
        }

        public async Task<bool> AggregateExistsAsync(
            string aggregateId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            using var session = _documentStore.OpenAsyncSession();

            // Use the index to check existence efficiently
            var exists = await session.Query<EventDocument, Events_ByAggregateIdAndVersion>()
                .AnyAsync(e => e.AggregateId == aggregateId, cancellationToken);

            return exists;
        }

        public async Task<int> GetAggregateVersionAsync(
            string aggregateId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            using var session = _documentStore.OpenAsyncSession();
            return await GetAggregateVersionInternalAsync(session, aggregateId, cancellationToken);
        }

        /// <summary>
        /// Internal method to get version within an existing session.
        /// Uses the Map-Reduce index for efficient lookup.
        /// </summary>
        private async Task<int> GetAggregateVersionInternalAsync(
            IAsyncDocumentSession session,
            string aggregateId,
            CancellationToken cancellationToken)
        {
            // Option 1: Use the Map-Reduce index (most efficient)
            var result = await session.Query<Events_CurrentVersionByAggregate.Result, Events_CurrentVersionByAggregate>()
                .Where(r => r.AggregateId == aggregateId)
                .FirstOrDefaultAsync(cancellationToken);

            if (result != null)
                return result.CurrentVersion;

            // Option 2: Fallback to direct query if index not ready (shouldn't happen in production)
            var maxVersion = await session.Query<EventDocument, Events_ByAggregateIdAndVersion>()
                .Where(e => e.AggregateId == aggregateId)
                .Select(e => e.Version)
                .OrderByDescending(v => v)
                .FirstOrDefaultAsync(cancellationToken);

            return maxVersion;
        }
    }
}
