using System;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.RavenDB.Examples;
using EventSourcing.RavenDB.Extensions;
using EventSourcing.RavenDB.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace EventSourcing.RavenDB.Examples
{
    /// <summary>
    /// Demonstrates how to use the Event Sourcing library.
    /// Shows configuration, aggregate creation, and repository usage.
    /// </summary>
    public class UsageExample
    {
        public static async Task Main(string[] args)
        {
            // ========================================================================
            // STEP 1: Configure Services (typically in Startup.cs or Program.cs)
            // ========================================================================
            
            var services = new ServiceCollection();

            // Configure event sourcing with RavenDB
            services.AddEventSourcing(options =>
            {
                options.Urls = new[] { "http://localhost:8080" };
                options.Database = "EventSourcingDemo";
            });

            // Or use the simplified overload
            // services.AddEventSourcing("http://localhost:8080", "EventSourcingDemo");

            // Register Product aggregate repository
            services.AddAggregateRepository<Product, ProductFactory>();

            var serviceProvider = services.BuildServiceProvider();

            // ========================================================================
            // STEP 2: Ensure Database and Indexes are Created
            // ========================================================================
            
            using (var scope = serviceProvider.CreateScope())
            {
                var documentStore = scope.ServiceProvider
                    .GetRequiredService<IDocumentStore>();
                
                // Ensure indexes exist and are ready
                documentStore.EnsureIndexesExist();
                
                Console.WriteLine("RavenDB connection established and indexes created");
            }

            // ========================================================================
            // STEP 3: Use the Repository to Create and Modify Aggregates
            // ========================================================================
            
            using (var scope = serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider
                    .GetRequiredService<IAggregateRepository<Product>>();

                // Create a new product
                var product = Product.Create(
                    name: "Laptop Computer",
                    price: 1299.99m,
                    category: "Electronics",
                    sku: "LAP-001"
                );

                // Add metadata to track who created it (useful for auditing)
                var createdEvent = product.GetUncommittedEvents().First();
                createdEvent.AddMetadata("UserId", "user-123");
                createdEvent.AddMetadata("CorrelationId", Guid.NewGuid().ToString());

                Console.WriteLine($"Created product: {product.Id}");

                // Save the product (persists events)
                await repository.SaveAsync(product);
                Console.WriteLine("Product saved successfully");

                // ========================================================================
                // STEP 4: Retrieve and Modify the Aggregate
                // ========================================================================

                var productId = product.Id;
                
                // Retrieve the product (reconstructed from events)
                var retrievedProduct = await repository.GetByIdAsync(productId);
                
                if (retrievedProduct != null)
                {
                    Console.WriteLine($"Retrieved: {retrievedProduct.Name} - ${retrievedProduct.Price}");
                    Console.WriteLine($"Current version: {retrievedProduct.Version}");

                    // Make some changes
                    retrievedProduct.UpdatePrice(1199.99m, "Holiday sale");
                    retrievedProduct.Restock(50, "Warehouse A");
                    retrievedProduct.Rename("Premium Laptop Computer");

                    // Save changes (persists new events)
                    await repository.SaveAsync(retrievedProduct);
                    Console.WriteLine("Product updated successfully");
                    Console.WriteLine($"New version: {retrievedProduct.Version}");
                }

                // ========================================================================
                // STEP 5: Demonstrate Event History
                // ========================================================================

                var eventStore = scope.ServiceProvider
                    .GetRequiredService<EventSourcing.RavenDB.Infrastructure.IEventStore>();

                var events = await eventStore.GetEventsAsync(productId);
                
                Console.WriteLine("\n=== Event History ===");
                foreach (var evt in events)
                {
                    Console.WriteLine($"Version {evt.Version}: {evt.EventType.Split('.').Last()} at {evt.Timestamp}");
                }

                // ========================================================================
                // STEP 6: Demonstrate Business Rule Enforcement
                // ========================================================================

                var productToDiscontinue = await repository.GetByIdAsync(productId);
                if (productToDiscontinue != null)
                {
                    // Discontinue the product
                    productToDiscontinue.Discontinue("End of product line");
                    await repository.SaveAsync(productToDiscontinue);

                    // Try to update price (should fail)
                    try
                    {
                        productToDiscontinue.UpdatePrice(999.99m, "Clearance");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"\n✓ Business rule enforced: {ex.Message}");
                    }
                }

                // ========================================================================
                // STEP 7: Time Travel - Reconstruct State at Any Point
                // ========================================================================

                Console.WriteLine("\n=== Time Travel ===");
                
                // Get state at version 2
                var eventsUpToV2 = await eventStore.GetEventsAsync(productId, fromVersion: 0);
                var eventsV2 = eventsUpToV2.Where(e => e.Version <= 2);
                
                var factory = scope.ServiceProvider
                    .GetRequiredService<IAggregateFactory<Product>>();
                var productAtV2 = factory.CreateFromHistory(productId, eventsV2);
                
                Console.WriteLine($"Product at version 2: {productAtV2.Name} - ${productAtV2.Price}");
                Console.WriteLine($"Stock quantity at version 2: {productAtV2.StockQuantity}");
            }

            Console.WriteLine("\n=== Demo Complete ===");
        }
    }
}

/* 
===============================================================================
USAGE IN ASP.NET CORE WEB API
===============================================================================

// In Program.cs or Startup.cs:

builder.Services.AddEventSourcing(options =>
{
    options.Urls = new[] { builder.Configuration["RavenDb:Urls"] };
    options.Database = builder.Configuration["RavenDb:Database"];
});

// Or use simplified configuration
builder.Services.AddEventSourcing(
    "http://localhost:8080", 
    "EventStore"
);

builder.Services.AddAggregateRepository<Product, ProductFactory>();


// In a Controller:

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IAggregateRepository<Product> _repository;

    public ProductsController(IAggregateRepository<Product> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = Product.Create(
            request.Name,
            request.Price,
            request.Category,
            request.Sku);

        await _repository.SaveAsync(product);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product.Id);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(string id)
    {
        var product = await _repository.GetByIdAsync(id);
        
        if (product == null)
            return NotFound();

        return Ok(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Category = product.Category,
            IsDiscontinued = product.IsDiscontinued
        });
    }

    [HttpPut("{id}/price")]
    public async Task<IActionResult> UpdatePrice(
        string id,
        [FromBody] UpdatePriceRequest request)
    {
        var product = await _repository.GetByIdAsync(id);
        
        if (product == null)
            return NotFound();

        try
        {
            product.UpdatePrice(request.NewPrice, request.Reason);
            await _repository.SaveAsync(product);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ConcurrencyException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(
        string id,
        [FromServices] IEventStore eventStore)
    {
        var events = await eventStore.GetEventsAsync(id);
        return Ok(events.Select(e => new
        {
            e.EventType,
            e.Version,
            e.Timestamp
        }));
    }
}

===============================================================================
ADVANCED PATTERNS
===============================================================================

1. CQRS WITH READ MODELS:
   - Create projections from events
   - Use separate read models for queries
   - Update read models asynchronously via event handlers

2. SAGA PATTERN:
   - Coordinate long-running business processes
   - Use events to trigger saga steps
   - Implement compensating transactions

3. EVENT VERSIONING:
   - Create new event versions for schema changes
   - Use upcasters to migrate old events
   - Never modify existing event schemas

4. SNAPSHOTS:
   - Implement ISnapshot interface
   - Use snapshot store for performance
   - Configure snapshot strategy

5. PROJECTIONS:
   - Subscribe to events
   - Build denormalized read models
   - Support multiple views of same data

===============================================================================
BEST PRACTICES
===============================================================================

1. ✓ Keep events immutable and past-tense
2. ✓ One aggregate per transaction
3. ✓ Use factories for aggregate creation from events
4. ✓ Validate business rules before raising events
5. ✓ Add metadata for correlation and causation
6. ✓ Handle concurrency conflicts gracefully
7. ✓ Test aggregates by checking emitted events
8. ✓ Never delete events (mark as superseded if needed)
9. ✓ Keep aggregates focused and bounded
10. ✓ Use snapshots for aggregates with many events

*/
