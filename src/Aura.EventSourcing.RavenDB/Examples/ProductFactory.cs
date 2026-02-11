using System;
using System.Collections.Generic;
using EventSourcing.RavenDB.Domain;
using EventSourcing.RavenDB.Infrastructure.Repositories;

namespace EventSourcing.RavenDB.Examples
{
    /// <summary>
    /// Factory for creating Product aggregate instances.
    /// Implements the Factory pattern required by the repository.
    /// </summary>
    public class ProductFactory : IAggregateFactory<Product>
    {
        public Product CreateFromHistory(string aggregateId, IEnumerable<IDomainEvent> events)
        {
            if (string.IsNullOrWhiteSpace(aggregateId))
                throw new ArgumentException("Aggregate ID cannot be null or empty", nameof(aggregateId));

            if (events == null)
                throw new ArgumentNullException(nameof(events));

            // Create empty product and load history
            var product = (Product)Activator.CreateInstance(typeof(Product), true)!;
            
            // Set the ID directly via reflection since constructor is private
            var idProperty = typeof(Product).BaseType!.GetProperty("Id");
            idProperty!.SetValue(product, aggregateId);
            
            // Load events to rebuild state
            product.LoadFromHistory(events);

            return product;
        }
    }
}
