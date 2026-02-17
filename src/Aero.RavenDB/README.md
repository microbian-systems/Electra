# Aero.RavenDB

RavenDB document store integration for the Aero framework with event sourcing support.

## Overview

`Aero.RavenDB` provides RavenDB implementations of the Aero repository interfaces. RavenDB is a NoSQL document database that offers ACID transactions, advanced querying, and built-in event sourcing capabilities.

## Key Components

### RavenDbRepositoryBase<T>

Base repository implementation for RavenDB:

```csharp
public abstract class RavenDbRepositoryBase<TEntity> : GenericRepository<TEntity, string>
    where TEntity : Entity
{
    protected readonly IAsyncDocumentSession Session;

    protected RavenDbRepositoryBase(IAsyncDocumentSession session, ILogger log) 
        : base(log)
    {
        Session = session;
    }

    public override async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await Session.Query<TEntity>().ToListAsync();
    }

    public override async Task<TEntity> FindByIdAsync(string id)
    {
        return await Session.LoadAsync<TEntity>(id);
    }

    public override async Task<TEntity> InsertAsync(TEntity entity)
    {
        entity.Id = entity.Id ?? Guid.NewGuid().ToString();
        entity.CreatedOn = DateTimeOffset.UtcNow;
        
        await Session.StoreAsync(entity);
        await Session.SaveChangesAsync();
        return entity;
    }

    // ... additional implementations
}
```

### AeroUserRepository

User management with RavenDB:

```csharp
public class AeroUserRepository : RavenDbRepositoryBase<AeroUser>, IAeroUserRepository
{
    public AeroUserRepository(IAsyncDocumentSession session, ILogger<AeroUserRepository> log) 
        : base(session, log) { }

    public async Task<AeroUser> FindByEmailAsync(string email)
    {
        return await Session.Query<AeroUser>()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<AeroUser> FindByUsernameAsync(string username)
    {
        return await Session.Query<AeroUser>()
            .FirstOrDefaultAsync(u => u.UserName == username);
    }
}
```

## Setup

### Configuration

```csharp
// appsettings.json
{
  "RavenDB": {
    "Urls": ["http://localhost:8080"],
    "DatabaseName": "Aero",
    "CertificatePath": null
  }
}

// Program.cs
builder.Services.AddRavenDb(builder.Configuration);

// Extension method
public static IServiceCollection AddRavenDb(this IServiceCollection services, IConfiguration config)
{
    var urls = config.GetSection("RavenDB:Urls").Get<string[]>();
    var databaseName = config["RavenDB:DatabaseName"];
    
    var documentStore = new DocumentStore
    {
        Urls = urls,
        Database = databaseName,
        Conventions = {
            FindCollectionName = type => type.Name
        }
    };
    
    documentStore.Initialize();
    
    services.AddSingleton<IDocumentStore>(documentStore);
    services.AddScoped<IAsyncDocumentSession>(sp => 
        sp.GetRequiredService<IDocumentStore>().OpenAsyncSession());
    
    services.AddScoped(typeof(IGenericRepository<>), typeof(RavenDbRepository<>));
    
    return services;
}
```

### Document Conventions

```csharp
public class Product
{
    public string Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    
    // RavenDB-specific: ignore for serialization
    [JsonIgnore]
    public string ComputedProperty => $"{Name} - ${Price}";
}
```

## Advanced Features

### Indexing

```csharp
// Static index
public class Products_ByCategory : AbstractIndexCreationTask<Product>
{
    public Products_ByCategory()
    {
        Map = products => from product in products
                          select new
                          {
                              product.CategoryId,
                              product.Name,
                              product.Price
                          };
    }
}

// Auto-indexes are created automatically for ad-hoc queries
var products = await Session.Query<Product>()
    .Where(p => p.Price > 100 && p.CategoryId == "electronics")
    .ToListAsync();
```

### Full-Text Search

```csharp
// Configure full-text search
public class Products_Search : AbstractIndexCreationTask<Product>
{
    public Products_Search()
    {
        Map = products => from product in products
                          select new
                          {
                              product.Name,
                              product.Description,
                              SearchQuery = new[] { product.Name, product.Description }
                          };
        
        Index(x => x.SearchQuery, FieldIndexing.Search);
    }
}

// Search
var results = await Session.Query<Product, Products_Search>()
    .Search(x => x.SearchQuery, searchTerm)
    .ToListAsync();
```

### Faceted Search

```csharp
// Define facets
var facets = new List<Facet>
{
    new Facet { FieldName = "CategoryId" },
    new Facet<_ProductPriceRange>
    {
        Ranges = new List<string>
        {
            "Price < 50",
            "Price >= 50 && Price < 100",
            "Price >= 100"
        }
    }
};

// Query with facets
var facetResults = await Session.Query<Product>()
    .Where(p => p.Name == searchTerm)
    .AggregateBy(facets)
    .ExecuteAsync();
```

### Patching

```csharp
// Update without loading document
await Session.Advanced.Patch<Product, decimal>(productId, 
    p => p.Price, 
    newPrice);

// Conditional patching
await Session.Advanced.Defer(new PatchCommandData(
    productId,
    null,
    new PatchRequest
    {
        Script = @"
            if (this.Stock > 0) {
                this.Stock -= args.quantity;
                this.LastSold = new Date();
            }
        ",
        Values = { { "quantity", 1 } }
    },
    null));
```

### Attachments

```csharp
// Store attachment
using (var fileStream = File.OpenRead("image.jpg"))
{
    Session.Advanced.Attachments.Store(productId, "image.jpg", fileStream, "image/jpeg");
    await Session.SaveChangesAsync();
}

// Retrieve attachment
var attachment = await Session.Advanced.Attachments.GetAsync(productId, "image.jpg");
using (var stream = attachment.Stream)
{
    // Process attachment
}
```

## Event Sourcing (RavenDB.ES)

RavenDB provides excellent support for event sourcing patterns.

### Aggregate Repository

```csharp
public class AggregateRepository<TAggregate> : IAggregateRepository<TAggregate>
    where TAggregate : AggregateBase, new()
{
    private readonly IAsyncDocumentSession _session;

    public async Task<TAggregate> LoadAsync(string id)
    {
        var aggregate = new TAggregate();
        var events = await _session.Query<Event>()
            .Where(e => e.AggregateId == id)
            .OrderBy(e => e.Version)
            .ToListAsync();
        
        aggregate.LoadFromEvents(events);
        return aggregate;
    }

    public async Task SaveAsync(TAggregate aggregate)
    {
        foreach (var @event in aggregate.UncommittedEvents)
        {
            await _session.StoreAsync(@event);
        }
        await _session.SaveChangesAsync();
    }
}
```

### Event Stream Projection

```csharp
public class OrderProjection : AbstractMultiMapIndexCreationTask<OrderView>
{
    public OrderProjection()
    {
        AddMap<OrderCreated>(events => from e in events
            select new OrderView
            {
                OrderId = e.OrderId,
                CustomerId = e.CustomerId,
                Total = 0,
                Status = "Created"
            });

        AddMap<OrderItemAdded>(events => from e in events
            select new OrderView
            {
                OrderId = e.OrderId,
                CustomerId = null,
                Total = e.Quantity * e.Price,
                Status = null
            });

        Reduce = results => from r in results
            group r by r.OrderId into g
            select new OrderView
            {
                OrderId = g.Key,
                CustomerId = g.First(x => x.CustomerId != null).CustomerId,
                Total = g.Sum(x => x.Total),
                Status = g.Last(x => x.Status != null).Status
            };
    }
}
```

## Bulk Operations

```csharp
// Bulk insert
using (var bulkInsert = documentStore.BulkInsert())
{
    foreach (var product in products)
    {
        await bulkInsert.StoreAsync(product);
    }
}

// Bulk delete
var operation = await documentStore.Operations.SendAsync(
    new DeleteByQueryOperation<Product>(p => p.IsDiscontinued));

await operation.WaitForCompletionAsync();
```

## Caching with Aggressive Mode

```csharp
// Cache query results aggressively
var products = await Session.Query<Product>()
    .Where(p => p.IsActive)
    .ToListAsync();

Session.Advanced.MaxNumberOfRequestsPerSession = 100;
Session.Advanced.UseOptimisticConcurrency = true;
```

## Subscriptions

```csharp
// Define subscription
var subscriptionName = await documentStore.Subscriptions.CreateAsync<Product>(
    p => p.Price > 1000);

// Process subscription
var worker = documentStore.Subscriptions.GetSubscriptionWorker<Product>(subscriptionName);

await worker.Run(async batch =>
{
    foreach (var item in batch.Items)
    {
        // Process high-value product
        await ProcessHighValueProduct(item.Result);
    }
});
```

## Best Practices

1. **Use Async Session** - Always use `IAsyncDocumentSession` for async operations
2. **Save Changes** - Call `SaveChangesAsync()` after write operations
3. **Index Strategically** - Create static indexes for frequent queries
4. **Batch Operations** - Use bulk insert for large data imports
5. **Optimistic Concurrency** - Enable for conflict detection
6. **Dispose Sessions** - Sessions are scoped and disposed automatically with DI

## Related Packages

- `Aero.Persistence.Core` - Repository interfaces
- `Aero.RavenDB.ES` - Event sourcing extensions
- `Aero.Events` - Domain events integration
- `Aero.Caching` - Caching decorators
