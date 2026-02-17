using System;
using EventSourcing.RavenDB.Infrastructure;
using EventSourcing.RavenDB.Infrastructure.Persistence;
using EventSourcing.RavenDB.Infrastructure.Repositories;
using EventSourcing.RavenDB.Infrastructure.Serialization;
using EventSourcing.RavenDB.Infrastructure.Snapshots;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace EventSourcing.RavenDB.Extensions
{
    /// <summary>
    /// Extension methods for configuring event sourcing with RavenDB.
    /// Follows the Builder pattern for fluent API configuration.
    /// Key differences from EF Core version:
    /// - Registers IDocumentStore instead of DbContext
    /// - No DbContextOptions needed
    /// - RavenDB-specific configuration options
    /// </summary>
    public static class EventSourcingServiceCollectionExtensions
    {
        /// <summary>
        /// Adds event sourcing infrastructure with RavenDB to the service collection
        /// </summary>
        public static IServiceCollection AddEventSourcing(
            this IServiceCollection services,
            Action<RavenDbOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Build RavenDB options
            var options = new RavenDbOptions();
            configureOptions(options);

            // Register IDocumentStore as a singleton (RavenDB best practice)
            // The document store is thread-safe and should be created once per application
            services.AddSingleton<IDocumentStore>(sp =>
            {
                var store = DocumentStoreFactory.CreateDocumentStore(options);
                return store;
            });

            // Register core services
            services.AddSingleton<IEventSerializer, JsonEventSerializer>();
            services.AddScoped<IEventStore, RavenDbEventStore>();

            // Register default snapshot strategy
            services.AddSingleton<ISnapshotStrategy>(new EventCountSnapshotStrategy(100));

            return services;
        }

        /// <summary>
        /// Adds event sourcing with explicit RavenDB URLs and database
        /// </summary>
        public static IServiceCollection AddEventSourcing(
            this IServiceCollection services,
            string[] urls,
            string database)
        {
            return services.AddEventSourcing(options =>
            {
                options.Urls = urls;
                options.Database = database;
            });
        }

        /// <summary>
        /// Adds event sourcing with single RavenDB URL and database
        /// </summary>
        public static IServiceCollection AddEventSourcing(
            this IServiceCollection services,
            string url,
            string database)
        {
            return services.AddEventSourcing(new[] { url }, database);
        }

        /// <summary>
        /// Registers a repository for a specific aggregate type
        /// </summary>
        public static IServiceCollection AddAggregateRepository<TAggregate, TFactory>(
            this IServiceCollection services)
            where TAggregate : class
            where TFactory : class, IAggregateFactory<TAggregate>
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddScoped<IAggregateFactory<TAggregate>, TFactory>();
            services.AddScoped<IAggregateRepository<TAggregate>, AggregateRepository<TAggregate>>();

            return services;
        }

        /// <summary>
        /// Configures a custom snapshot strategy
        /// </summary>
        public static IServiceCollection AddSnapshotStrategy<TStrategy>(
            this IServiceCollection services)
            where TStrategy : class, ISnapshotStrategy
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddSingleton<ISnapshotStrategy, TStrategy>();

            return services;
        }

        /// <summary>
        /// Configures a custom event serializer
        /// </summary>
        public static IServiceCollection AddEventSerializer<TSerializer>(
            this IServiceCollection services)
            where TSerializer : class, IEventSerializer
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Replace the default serializer
            services.AddSingleton<IEventSerializer, TSerializer>();

            return services;
        }
    }

    /// <summary>
    /// Extension methods for IDocumentStore maintenance and utilities
    /// </summary>
    public static class DocumentStoreExtensions
    {
        /// <summary>
        /// Ensures all indexes are created and waits for them to become non-stale.
        /// Useful for testing and initial setup.
        /// </summary>
        public static void EnsureIndexesExist(this IDocumentStore documentStore)
        {
            if (documentStore == null)
                throw new ArgumentNullException(nameof(documentStore));

            // Indexes are already created in DocumentStoreFactory.CreateIndexes
            // This method can be used to wait for indexes if needed
            using var session = documentStore.OpenSession();
            
            // Query each index to ensure it exists
            session.Query<EventDocument, Persistence.Indexes.Events_ByAggregateIdAndVersion>()
                .Take(0)
                .ToList();
            
            session.Query<EventDocument, Persistence.Indexes.Events_ByTimestamp>()
                .Take(0)
                .ToList();
            
            session.Query<EventDocument, Persistence.Indexes.Events_ByEventType>()
                .Take(0)
                .ToList();
        }

        /// <summary>
        /// Waits for all indexes to become non-stale.
        /// Useful in testing scenarios.
        /// </summary>
        public static void WaitForIndexing(
            this IDocumentStore documentStore, 
            TimeSpan? timeout = null)
        {
            if (documentStore == null)
                throw new ArgumentNullException(nameof(documentStore));

            using var session = documentStore.OpenSession();
            session.Advanced.DocumentStore.WaitForIndexing(
                timeout: timeout ?? TimeSpan.FromSeconds(30));
        }
    }
}
