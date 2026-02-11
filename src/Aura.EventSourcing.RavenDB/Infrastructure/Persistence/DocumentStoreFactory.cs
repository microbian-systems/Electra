using System;
using System.Security.Cryptography.X509Certificates;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;

namespace EventSourcing.RavenDB.Infrastructure.Persistence
{
    /// <summary>
    /// Configuration options for RavenDB document store.
    /// Provides fluent API for configuring database connection.
    /// </summary>
    public class RavenDbOptions
    {
        /// <summary>
        /// RavenDB server URLs (can be multiple for cluster setup)
        /// </summary>
        public string[] Urls { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Database name to use
        /// </summary>
        public string Database { get; set; } = "EventStore";

        /// <summary>
        /// Optional certificate for secure connections
        /// </summary>
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// Whether to create the database if it doesn't exist
        /// </summary>
        public bool CreateDatabaseIfNotExists { get; set; } = true;

        /// <summary>
        /// Custom document conventions (optional)
        /// </summary>
        public Action<DocumentConventions>? ConfigureConventions { get; set; }
    }

    /// <summary>
    /// Factory for creating and configuring RavenDB document store.
    /// Implements the Factory pattern with Singleton behavior for the store.
    /// </summary>
    public static class DocumentStoreFactory
    {
        /// <summary>
        /// Creates a configured IDocumentStore instance
        /// </summary>
        public static IDocumentStore CreateDocumentStore(RavenDbOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (options.Urls == null || options.Urls.Length == 0)
                throw new ArgumentException("At least one URL must be provided", nameof(options.Urls));

            if (string.IsNullOrWhiteSpace(options.Database))
                throw new ArgumentException("Database name must be provided", nameof(options.Database));

            var store = new DocumentStore
            {
                Urls = options.Urls,
                Database = options.Database,
                Certificate = options.Certificate
            };

            // Apply custom conventions if provided
            if (options.ConfigureConventions != null)
            {
                options.ConfigureConventions(store.Conventions);
            }
            else
            {
                // Apply default conventions for event sourcing
                ConfigureDefaultConventions(store.Conventions);
            }

            store.Initialize();

            // Create database if needed
            if (options.CreateDatabaseIfNotExists)
            {
                EnsureDatabaseExists(store, options.Database);
            }

            // Create indexes
            CreateIndexes(store);

            return store;
        }

        private static void ConfigureDefaultConventions(DocumentConventions conventions)
        {
            // Use camelCase for JSON properties (matches System.Text.Json default)
            conventions.PropertyNameConverter = propertyInfo => 
                char.ToLowerInvariant(propertyInfo.Name[0]) + propertyInfo.Name.Substring(1);

            // Configure identity generation for EventDocument
            conventions.FindCollectionName = type =>
            {
                if (type == typeof(EventDocument))
                    return "Events";
                
                return DocumentConventions.DefaultGetCollectionName(type);
            };

            // Use Guid-based IDs for events
            conventions.FindIdentityProperty = memberInfo =>
                memberInfo.Name == "Id";
        }

        private static void EnsureDatabaseExists(IDocumentStore store, string databaseName)
        {
            try
            {
                var databaseRecord = store.Maintenance.Server.Send(
                    new Raven.Client.ServerWide.Operations.GetDatabaseRecordOperation(databaseName));
                
                if (databaseRecord == null)
                {
                    store.Maintenance.Server.Send(
                        new Raven.Client.ServerWide.Operations.CreateDatabaseOperation(
                            new Raven.Client.ServerWide.DatabaseRecord(databaseName)));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to ensure database '{databaseName}' exists", ex);
            }
        }

        private static void CreateIndexes(IDocumentStore store)
        {
            // Create indexes for efficient querying
            // RavenDB auto-indexes will handle most cases, but we can create specific ones
            IndexCreation.CreateIndexes(typeof(DocumentStoreFactory).Assembly, store);
        }
    }
}
