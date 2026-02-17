# Event Sourcing Library for .NET with RavenDB

A production-ready event sourcing library for .NET applications using RavenDB. This library implements event sourcing patterns with strong adherence to SOLID principles and established design patterns, leveraging RavenDB's document database capabilities.

## 🎯 Features

- **Complete Event Store**: RavenDB-based event persistence with optimistic concurrency control
- **Document-Based Storage**: Leverages RavenDB's native JSON document storage
- **Efficient Indexing**: Map-Reduce indexes for high-performance queries
- **Aggregate Pattern**: DDD-style aggregate roots with event sourcing
- **Repository Pattern**: Generic repository for aggregate persistence
- **Snapshot Support**: Performance optimization for long event streams
- **SOLID Principles**: Clean architecture following best practices
- **Extensible**: Strategy pattern for serialization and snapshots
- **Type-Safe**: Strong typing with generic constraints
- **Production Ready**: Transaction support, concurrency handling, and error management
- **No Schema Migrations**: RavenDB's schemaless design eliminates migration complexity

## 🏗️ Architecture & Design Patterns

### Why RavenDB for Event Sourcing?

RavenDB is an excellent choice for event sourcing because:

1. **Document Model**: Events are naturally represented as JSON documents
2. **No Schema Migrations**: Add fields to events without database changes
3. **Built-in Indexes**: Map-Reduce indexes for efficient aggregation
4. **ACID Transactions**: Per-aggregate consistency guarantees
5. **Optimistic Concurrency**: Native support with ETags
6. **High Performance**: Fast writes and efficient event retrieval
7. **Scalability**: Distributed architecture with sharding support
8. **Time Travel**: Built-in document versioning and revisions

### Key Differences from EF Core Version

| Aspect | EF Core | RavenDB |
|--------|---------|---------|
| Storage Model | Relational tables | JSON documents |
| Schema | Fixed schema with migrations | Schemaless, flexible |
| Indexes | SQL indexes | Map-Reduce indexes |
| Concurrency | Version column | ETags |
| Transactions | DbContext SaveChanges | Session SaveChanges |
| Queries | LINQ to SQL | LINQ to Documents |
| Singleton | DbContext (scoped) | IDocumentStore (singleton) |

## 📦 Installation

```bash
# Add the library to your project
dotnet add reference EventSourcing.RavenDB.csproj

# Required dependencies
dotnet add package RavenDB.Client
```

## 🚀 Quick Start

### 1. Configure Services

```csharp
// In Program.cs or Startup.cs
services.AddEventSourcing(options =>
{
    options.Urls = new[] { "http://localhost:8080" };
    options.Database = "EventStore";
});

// Or use simplified configuration
services.AddEventSourcing("http://localhost:8080", "EventStore");

// Register your aggregate repository
services.AddAggregateRepository<Product, ProductFactory>();
```

### 2. Create Domain Events

```csharp
public class ProductCreatedEvent : DomainEventBase
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}

public class ProductPriceUpdatedEvent : DomainEventBase
{
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
}
```

### 3. Create Aggregate Root

```csharp
public class Product : AggregateRootBase
{
    private string _name;
    private decimal _price;
    private bool _isDiscontinued;

    public string Name => _name;
    public decimal Price => _price;
    public bool IsDiscontinued => _isDiscontinued;

    // Factory method for creation
    public static Product Create(string name, decimal price, string category)
    {
        var product = new Product();
        var @event = new ProductCreatedEvent
        {
            Name = name,
            Price = price,
            Category = category
        };
        product.RaiseEvent(@event);
        return product;
    }

    // Business methods
    public void UpdatePrice(decimal newPrice)
    {
        if (_isDiscontinued)
            throw new InvalidOperationException("Cannot update discontinued product");
            
        var @event = new ProductPriceUpdatedEvent
        {
            OldPrice = _price,
            NewPrice = newPrice
        };
        RaiseEvent(@event);
    }

    // Event application
    protected override void ApplyEventCore(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProductCreatedEvent created:
                _name = created.Name;
                _price = created.Price;
                break;
            case ProductPriceUpdatedEvent updated:
                _price = updated.NewPrice;
                break;
        }
    }
}
```

### 4. Use in Your Application

```csharp
public class ProductService
{
    private readonly IAggregateRepository<Product> _repository;

    public ProductService(IAggregateRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<string> CreateProduct(string name, decimal price, string category)
    {
        var product = Product.Create(name, price, category);
        await _repository.SaveAsync(product);
        return product.Id;
    }

    public async Task UpdateProductPrice(string productId, decimal newPrice)
    {
        var product = await _repository.GetByIdAsync(productId);
        if (product == null)
            throw new ArgumentException("Product not found");

        product.UpdatePrice(newPrice);
        await _repository.SaveAsync(product);
    }
}
```

## 🔧 RavenDB-Specific Features

### Map-Reduce Indexes

The library uses RavenDB's powerful Map-Reduce indexes:

```csharp
// Get current version efficiently using Map-Reduce
public class Events_CurrentVersionByAggregate : AbstractIndexCreationTask<EventDocument>
{
    public Events_CurrentVersionByAggregate()
    {
        Map = events => from evt in events
                       select new { evt.AggregateId, CurrentVersion = evt.Version };
        
        Reduce = results => from result in results
                           group result by result.AggregateId into g
                           select new { AggregateId = g.Key, CurrentVersion = g.Max(x => x.CurrentVersion) };
    }
}
```

### Document Store Configuration

```csharp
services.AddEventSourcing(options =>
{
    // Multiple URLs for cluster setup
    options.Urls = new[] 
    { 
        "http://node1.ravendb.local:8080",
        "http://node2.ravendb.local:8080" 
    };
    
    options.Database = "EventStore";
    
    // Use certificate for secure connections
    options.Certificate = new X509Certificate2("path/to/cert.pfx", "password");
    
    // Custom conventions
    options.ConfigureConventions = conventions =>
    {
        conventions.MaxNumberOfRequestsPerSession = 50;
        // Additional custom conventions
    };
});
```

### Waiting for Indexes

For testing or initial setup:

```csharp
var documentStore = serviceProvider.GetRequiredService<IDocumentStore>();

// Ensure indexes are created
documentStore.EnsureIndexesExist();

// Wait for indexing to complete (useful in tests)
documentStore.WaitForIndexing(timeout: TimeSpan.FromSeconds(30));
```

## 📊 RavenDB Storage Model

### Event Document Structure

```json
{
  "EventId": "guid",
  "AggregateId": "product-123",
  "EventType": "ProductCreatedEvent, Assembly",
  "EventData": "{\"name\":\"Laptop\",\"price\":1299.99}",
  "Metadata": "{\"userId\":\"user-123\"}",
  "Version": 1,
  "Timestamp": "2025-02-05T10:30:00Z",
  "CreatedAt": "2025-02-05T10:30:00Z",
  "@metadata": {
    "@collection": "Events",
    "@id": "events/1-A"
  }
}
```

### Indexes Created

1. **Events_ByAggregateIdAndVersion**: Fast lookup by aggregate
2. **Events_ByTimestamp**: Temporal queries
3. **Events_ByEventType**: Event type filtering
4. **Events_CurrentVersionByAggregate**: Map-Reduce for current version

## 🎯 When to Use RavenDB vs EF Core

### Choose RavenDB When:
- ✅ You need schemaless flexibility
- ✅ Event schema evolution is important
- ✅ You want built-in document versioning
- ✅ You need distributed/sharded storage
- ✅ You prefer NoSQL document model
- ✅ You want to avoid schema migrations

### Choose EF Core When:
- ✅ You have existing SQL infrastructure
- ✅ You need complex relational queries
- ✅ Your team is more familiar with SQL
- ✅ You want strong typing at database level
- ✅ You need SQL-specific features

## 🔒 Concurrency Handling

RavenDB uses ETags for optimistic concurrency:

```csharp
try
{
    await repository.SaveAsync(product);
}
catch (ConcurrencyException ex)
{
    // Handle conflict:
    // 1. Reload aggregate
    // 2. Retry operation
    // 3. Or notify user of conflict
    
    var fresh = await repository.GetByIdAsync(product.Id);
    // Merge or retry
}
```

## 📈 Performance Considerations

1. **Use Indexes Wisely**: RavenDB creates auto-indexes, but custom indexes are more efficient
2. **Session Per Request**: Open a new session for each request (scoped)
3. **Batch Operations**: Use SaveChanges to batch multiple operations
4. **Snapshots**: For aggregates with 100+ events
5. **Caching**: RavenDB has built-in caching
6. **Sharding**: Distribute aggregates across multiple nodes for scale

## 🧪 Testing with RavenDB

RavenDB provides an embedded test driver:

```csharp
[Fact]
public async Task SaveEvents_ShouldPersistToRavenDB()
{
    // Arrange
    using var testDriver = new RavenTestDriver();
    var documentStore = testDriver.GetDocumentStore();
    
    var serializer = new JsonEventSerializer();
    var eventStore = new RavenDbEventStore(documentStore, serializer);
    
    var product = Product.Create("Laptop", 1299.99m, "Electronics");
    
    // Act
    await eventStore.SaveEventsAsync(
        product.Id, 
        product.GetUncommittedEvents(), 
        0);
    
    // Assert
    var events = await eventStore.GetEventsAsync(product.Id);
    Assert.Single(events);
}
```

## 🏛️ Data Structures

### Document Collections (RavenDB)
- **Events Collection**: All event documents
- **Indexes**: Map-Reduce structures for aggregation

### Query Patterns
- **By Aggregate**: O(log n) using index
- **By Version Range**: O(k) where k = number of events in range
- **Current Version**: O(1) using Map-Reduce index

## 🔄 Migration from EF Core Version

If migrating from the EF Core version:

1. **Domain Layer**: No changes needed (100% compatible)
2. **Infrastructure**: Replace DbContext with IDocumentStore
3. **Configuration**: Update service registration
4. **Data Migration**: Export events from SQL, import to RavenDB

```csharp
// Old (EF Core)
services.AddEventSourcing(options =>
    options.UseSqlServer(connectionString));

// New (RavenDB)
services.AddEventSourcing(options =>
{
    options.Urls = new[] { ravenDbUrl };
    options.Database = databaseName;
});
```

## 📚 RavenDB Resources

- [RavenDB Documentation](https://ravendb.net/docs)
- [RavenDB .NET Client](https://ravendb.net/docs/article-page/6.0/csharp)
- [RavenDB Indexes](https://ravendb.net/docs/article-page/6.0/csharp/indexes/what-are-indexes)
- [RavenDB Best Practices](https://ravendb.net/docs/article-page/6.0/csharp/best-practices)

## 🎓 Learning Outcomes

This implementation demonstrates:
- ✅ Event Sourcing with document databases
- ✅ RavenDB Map-Reduce indexes
- ✅ Document-based persistence patterns
- ✅ NoSQL event store implementation
- ✅ Schemaless event evolution
- ✅ SOLID principles (same as EF version)
- ✅ Clean architecture
- ✅ Production-ready RavenDB usage

## 🚀 Running RavenDB

### Using Docker

```bash
docker run -d \
  -p 8080:8080 \
  -p 38888:38888 \
  --name ravendb \
  ravendb/ravendb

# Access RavenDB Studio at http://localhost:8080
```

### Configuration in appsettings.json

```json
{
  "RavenDb": {
    "Urls": ["http://localhost:8080"],
    "Database": "EventStore"
  }
}
```

## 🤝 Contributing

Contributions are welcome! Please ensure:
- SOLID principles are maintained
- RavenDB best practices are followed
- Existing patterns are followed
- Unit tests are included
- Documentation is updated

## 📄 License

[Your License Here]

## 🙏 Acknowledgments

Based on event sourcing best practices and adapted for RavenDB's document model.
