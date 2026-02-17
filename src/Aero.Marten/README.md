# Aero.Marten

Marten PostgreSQL document store integration for the Aero framework.

## Overview

`Aero.Marten` provides Marten-based implementations of the Aero repository interfaces. Marten turns PostgreSQL into a .NET transactional document database and event store, combining the flexibility of NoSQL with the reliability of PostgreSQL.

## Key Components

### GenericMartenRepository<T, TKey>

Base repository implementation for Marten:

```csharp
public class GenericMartenRepository<T, TKey> : GenericRepository<T, TKey>, 
    IGenericMartenRepository<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected readonly IDocumentSession Session;

    public GenericMartenRepository(IDocumentSession session, ILogger log) : base(log)
    {
        Session = session;
    }

    public override async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Session.Query<T>().ToListAsync();
    }

    public override async Task<T> FindByIdAsync(TKey id)
    {
        return await Session.LoadAsync<T>(id);
    }

    public override async Task<T> InsertAsync(T entity)
    {
        entity.Id = entity.Id ?? GenerateKey();
        entity.CreatedOn = DateTimeOffset.UtcNow;
        
        Session.Store(entity);
        await Session.SaveChangesAsync();
        return entity;
    }

    public override async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await Session.Query<T>().Where(predicate).ToListAsync();
    }

    // ... additional implementations
}
```

### DynamicMartenRepository

Dynamic/reusable repository for ad-hoc operations:

```csharp
public class DynamicMartenRepository
{
    private readonly IDocumentSession _session;
    private readonly ILogger<DynamicMartenRepository> _log;

    public DynamicMartenRepository(IDocumentSession session, ILogger<DynamicMartenRepository> log)
    {
        _session = session;
        _log = log;
    }

    public async Task<T> GetByIdAsync<T>(string id) where T : class
    {
        return await _session.LoadAsync<T>(id);
    }

    public async Task StoreAsync<T>(T document) where T : class
    {
        _session.Store(document);
        await _session.SaveChangesAsync();
    }

    public IQueryable<T> Query<T>() where T : class
    {
        return _session.Query<T>();
    }
}
```

## Setup

### Configuration

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "Marten": "Host=localhost;Database=aero;Username=aero;Password=password"
  }
}

// Program.cs
builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("Marten"));
    
    // Configure document types
    options.Schema.For<Product>().Index(x => x.CategoryId);
    options.Schema.For<Order>().SoftDeleted();
    
    // Event store configuration
    options.Events.AddEventTypes(new[] { 
        typeof(OrderCreated), 
        typeof(OrderItemAdded),
        typeof(OrderSubmitted) 
    });
    
    // Enable artifact cleaning
    options.AutoCreateSchemaObjects = AutoCreate.All;
});

// Register repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericMartenRepository<>));
services.AddScoped<DynamicMartenRepository>();
```

### Document Store Configuration

```csharp
public class MartenConfig : MartenRegistry
{
    public MartenConfig()
    {
        // Global configurations
        For<Product>().Duplicate(p => p.CategoryId);
        For<Order>().GinIndexJsonData();
        For<Customer>().FullTextIndex();
        
        // Soft deletes
        For<AuditLog>().SoftDeleted();
    }
}
```

## Advanced Features

### LINQ Support

Marten provides extensive LINQ support:

```csharp
// Basic queries
var products = await Session.Query<Product>()
    .Where(p => p.Price > 100)
    .ToListAsync();

// Complex predicates
var orders = await Session.Query<Order>()
    .Where(o => o.Items.Any(i => i.Price > 1000))
    .Where(o => o.OrderDate > DateTime.Now.AddMonths(-1))
    .OrderBy(o => o.Total)
    .Take(10)
    .ToListAsync();

// Includes (similar to EF Core)
var productsWithCategories = await Session.Query<Product>()
    .Include<Product, Category>(p => p.CategoryId, categories)
    .ToListAsync();
```

### Full-Text Search

```csharp
// Configure full-text index
options.Schema.For<Product>().FullTextIndex();

// Search
var results = await Session.Query<Product>()
    .Where(p => p.SearchVector.PlainTextSearch("electronics"))
    .ToListAsync();

// Weighted search
var rankedResults = await Session.Query<Product>()
    .Where(p => p.SearchVector.WebStyleSearch("laptop computer"))
    .OrderBy(p => p.SearchVector.Rank)
    .ToListAsync();
```

### JSONB Operations

```csharp
// Store complex objects as JSONB
public class Product : Entity
{
    public string Name { get; set; }
    public ProductMetadata Metadata { get; set; }
}

// Query JSON properties
var products = await Session.Query<Product>()
    .Where(p => p.Metadata.Tags.Contains("featured"))
    .ToListAsync();

// JSON path queries
var products = await Session.Query<Product>()
    .Where(p => p.Metadata.JsonPath("$.Attributes.Color") == "Red")
    .ToListAsync();
```

### Event Sourcing

Marten has built-in event sourcing capabilities:

```csharp
// Event types
public record OrderCreated(string OrderId, string CustomerId);
public record OrderItemAdded(string OrderId, string ProductId, int Quantity, decimal Price);
public record OrderSubmitted(string OrderId);

// Aggregate
public class Order : Aggregate
{
    public string CustomerId { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();
    public bool IsSubmitted { get; private set; }

    public void Apply(OrderCreated @event)
    {
        CustomerId = @event.CustomerId;
    }

    public void Apply(OrderItemAdded @event)
    {
        Items.Add(new OrderItem(@event.ProductId, @event.Quantity, @event.Price));
    }

    public void Apply(OrderSubmitted @event)
    {
        IsSubmitted = true;
    }
}

// Usage
var order = new Order();
order.Start(orderId, customerId);
order.AddItem(productId, quantity, price);
order.Submit();

Session.Events.Append(orderId, order.UncommittedEvents);
await Session.SaveChangesAsync();
```

### Projections

```csharp
// Live aggregation
var orderView = await Session.Events.AggregateStreamAsync<Order>(orderId);

// Inline projection
options.Projections.Add<OrderView>(ProjectionLifecycle.Inline);

// Async projection
options.Projections.Add<OrderSummary>(ProjectionLifecycle.Async);
```

### Batching

```csharp
// Bulk insert
var documents = GetProductsToInsert();
await Session.StoreAsync(documents);
await Session.SaveChangesAsync();

// Batch querying
var batch = Session.CreateBatchQuery();
var productsQuery = batch.Query<Product>().Where(p => p.CategoryId == "electronics");
var ordersQuery = batch.Query<Order>().Where(o => o.CustomerId == customerId);

await batch.Execute();
var products = await productsQuery;
var orders = await ordersQuery;
```

## Multi-Tenancy

```csharp
// Configure multi-tenancy
options.Connection(connectionString);
options.Policies.AllDocumentsAreMultiTenanted();

// Or per-document
options.Schema.For<Product>().MultiTenanted();

// Query with tenant
using (Session.SetTenantId("tenant-1"))
{
    var products = await Session.Query<Product>().ToListAsync();
}
```

## Soft Deletes

```csharp
// Configure soft delete
options.Schema.For<Product>().SoftDeleted();

// Soft delete an entity
Session.Delete<Product>(productId);
await Session.SaveChangesAsync();

// Query including deleted
var allProducts = await Session.Query<Product>()
    .MaybeDeleted()
    .ToListAsync();

// Restore
Session.UndoDelete<Product>(productId);
await Session.SaveChangesAsync();
```

## Custom SQL

```csharp
// Raw SQL query
var results = await Session.QueryAsync<Product>(
    "SELECT * FROM mt_doc_product WHERE data->>'CategoryId' = ?",
    categoryId);

// Stored procedure
await Session.CallFunction("update_product_prices", categoryId, percentage);
```

## Best Practices

1. **Use DocumentSession per Request** - Scoped lifetime with DI
2. **Call SaveChanges** - Always call after modifications
3. **Index Strategically** - Add indexes for frequent queries
4. **Use Async Methods** - All Marten operations support async
5. **Handle Concurrency** - Use optimistic concurrency for critical data
6. **Schema Migration** - Use AutoCreate.All in development, migrations in production

## Related Packages

- `Aero.Persistence.Core` - Repository interfaces
- `Aero.Persistence` - Base repository implementations
- `Aero.EfCore` - Entity Framework alternative
- `Aero.Events` - Event sourcing integration
