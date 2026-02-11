# Event Sourcing with RavenDB - Implementation Summary

## 📦 What Has Been Created

A complete, production-ready event sourcing library for .NET applications using RavenDB, built with strong adherence to SOLID principles and incorporating multiple design patterns. This version shares **100% of the domain layer** with the EF Core version, demonstrating clean architecture and the power of abstraction.

## 🗂️ Project Structure

```
EventSourcing.RavenDB/
│
├── Domain/                              # IDENTICAL to EF Core version
│   ├── IDomainEvent.cs                 # Event interface (ISP)
│   ├── DomainEventBase.cs              # Event base class (Template Method)
│   ├── IAggregateRoot.cs               # Aggregate interface (DDD)
│   └── AggregateRootBase.cs            # Aggregate base implementation
│
├── Infrastructure/                      # RavenDB-specific implementation
│   ├── IEventStore.cs                  # Event store interface (same contract)
│   ├── RavenDbEventStore.cs            # RavenDB implementation
│   ├── ConcurrencyException.cs         # Domain exception (identical)
│   │
│   ├── Persistence/
│   │   ├── EventDocument.cs            # RavenDB document model
│   │   ├── DocumentStoreFactory.cs     # IDocumentStore factory
│   │   └── Indexes/
│   │       └── EventIndexes.cs         # Map-Reduce indexes
│   │
│   ├── Serialization/                  # IDENTICAL to EF Core version
│   │   ├── IEventSerializer.cs         # Serializer interface (Strategy)
│   │   └── JsonEventSerializer.cs      # JSON implementation
│   │
│   ├── Repositories/                   # IDENTICAL to EF Core version
│   │   ├── IAggregateRepository.cs     # Repository interface
│   │   ├── AggregateRepository.cs      # Generic implementation
│   │   └── IAggregateFactory.cs        # Factory interface
│   │
│   └── Snapshots/                      # IDENTICAL to EF Core version
│       ├── ISnapshot.cs                # Snapshot interface (Memento)
│       ├── ISnapshotStore.cs           # Snapshot persistence
│       └── ISnapshotStrategy.cs        # Strategy implementations
│
├── Extensions/
│   └── EventSourcingServiceCollectionExtensions.cs  # RavenDB DI setup
│
├── Examples/                            # Updated for RavenDB
│   ├── ProductAggregate.cs             # Example aggregate (identical)
│   ├── ProductFactory.cs               # Example factory (identical)
│   └── UsageExample.cs                 # RavenDB-specific usage
│
├── EventSourcing.RavenDB.csproj        # Project with RavenDB dependencies
├── README.md                            # RavenDB-specific documentation
└── COMPARISON.md                        # EF Core vs RavenDB comparison
```

## 🎯 What's Different from EF Core Version?

### Identical Components (100% Reusable)

✅ **Entire Domain Layer**
- All interfaces and base classes
- Aggregate root pattern
- Event base classes
- Business logic patterns

✅ **Repository Pattern**
- Interface unchanged
- Generic implementation unchanged
- Factory pattern unchanged

✅ **Serialization**
- Interface and implementation identical
- JSON serialization logic same

✅ **Snapshots**
- All interfaces and strategies identical

### RavenDB-Specific Components

🔄 **Persistence Layer**
- `EventDocument` instead of `EventEntity`
- `IDocumentStore` instead of `DbContext`
- `RavenDbEventStore` instead of `EfCoreEventStore`

🔄 **Indexes**
- Map-Reduce indexes instead of SQL indexes
- `AbstractIndexCreationTask` pattern
- Four specialized indexes for different query patterns

🔄 **Configuration**
- `RavenDbOptions` instead of `DbContextOptions`
- `DocumentStoreFactory` for store creation
- Different service registration

## 🏗️ RavenDB-Specific Design Patterns

### 1. Document Store Pattern (Singleton)

```csharp
// IDocumentStore is registered as singleton (thread-safe)
services.AddSingleton<IDocumentStore>(sp =>
{
    var store = DocumentStoreFactory.CreateDocumentStore(options);
    return store;
});

// IDocumentSession is created per request (scoped)
using var session = _documentStore.OpenAsyncSession();
```

**Why Singleton?**
- IDocumentStore is expensive to create
- Thread-safe by design
- Connection pooling
- Index caching

### 2. Map-Reduce Pattern (Indexes)

```csharp
public class Events_CurrentVersionByAggregate : AbstractIndexCreationTask<EventDocument>
{
    public Events_CurrentVersionByAggregate()
    {
        // Map: Extract data from each document
        Map = events => from evt in events
                       select new { evt.AggregateId, CurrentVersion = evt.Version };
        
        // Reduce: Aggregate by key
        Reduce = results => from result in results
                           group result by result.AggregateId into g
                           select new { AggregateId = g.Key, CurrentVersion = g.Max(x => x.CurrentVersion) };
    }
}
```

**Benefits:**
- O(1) lookups for aggregate version
- Automatically maintained by RavenDB
- Distributed computation

### 3. Session-Based Unit of Work

```csharp
using var session = _documentStore.OpenAsyncSession();
session.Advanced.UseOptimisticConcurrency = true;

// Multiple operations in one unit of work
await session.StoreAsync(doc1);
await session.StoreAsync(doc2);

// Atomic commit
await session.SaveChangesAsync();
```

## 📊 RavenDB Indexes Explained

### 1. Events_ByAggregateIdAndVersion
**Purpose**: Fast event retrieval by aggregate
**Type**: Simple map index
**Use Case**: Loading aggregate history

```csharp
// Usage
var events = await session.Query<EventDocument, Events_ByAggregateIdAndVersion>()
    .Where(e => e.AggregateId == id && e.Version >= fromVersion)
    .OrderBy(e => e.Version)
    .ToListAsync();
```

### 2. Events_ByTimestamp
**Purpose**: Temporal queries
**Type**: Simple map index
**Use Case**: Event replay, debugging, auditing

```csharp
// Usage
var recentEvents = await session.Query<EventDocument, Events_ByTimestamp>()
    .Where(e => e.Timestamp >= yesterday)
    .ToListAsync();
```

### 3. Events_ByEventType
**Purpose**: Event type filtering
**Type**: Simple map index
**Use Case**: Projections, event handlers

```csharp
// Usage
var priceEvents = await session.Query<EventDocument, Events_ByEventType>()
    .Where(e => e.EventType == "ProductPriceUpdatedEvent")
    .ToListAsync();
```

### 4. Events_CurrentVersionByAggregate
**Purpose**: Efficient version lookup
**Type**: Map-Reduce index
**Use Case**: Optimistic concurrency checks

```csharp
// Usage
var versionInfo = await session.Query<Result, Events_CurrentVersionByAggregate>()
    .Where(r => r.AggregateId == id)
    .FirstOrDefaultAsync();

var currentVersion = versionInfo?.CurrentVersion ?? 0;
```

## 🎯 Key Advantages of RavenDB Version

### 1. No Schema Migrations

**EF Core:**
```bash
dotnet ef migrations add AddNewEventField
dotnet ef database update
```

**RavenDB:**
```csharp
// Just add the field to your event class
public class ProductCreatedEvent : DomainEventBase
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string NewField { get; set; } // ← Just add it!
}
// No migration needed!
```

### 2. Built-in Document Versioning

RavenDB tracks document changes automatically:
```csharp
// Get document history
var revisions = await session.Advanced.Revisions
    .GetForAsync<EventDocument>(documentId);
```

### 3. Distributed by Default

```csharp
services.AddEventSourcing(options =>
{
    // Multiple nodes for high availability
    options.Urls = new[] 
    { 
        "http://node1:8080",
        "http://node2:8080",
        "http://node3:8080"
    };
});
```

### 4. Flexible Querying

```csharp
// Complex queries with LINQ
var events = await session.Query<EventDocument>()
    .Where(e => e.AggregateId.StartsWith("product") 
             && e.Timestamp >= DateTime.UtcNow.AddDays(-7)
             && e.EventType.Contains("Price"))
    .OrderByDescending(e => e.Timestamp)
    .ToListAsync();
```

## 💡 Usage Examples

### Basic Setup

```csharp
// Startup configuration
services.AddEventSourcing("http://localhost:8080", "EventStore");
services.AddAggregateRepository<Product, ProductFactory>();

// Use in service
public class ProductService
{
    private readonly IAggregateRepository<Product> _repository;
    
    public async Task CreateProduct(string name, decimal price)
    {
        var product = Product.Create(name, price, "Electronics");
        await _repository.SaveAsync(product);
    }
}
```

### Advanced Configuration

```csharp
services.AddEventSourcing(options =>
{
    options.Urls = new[] { configuration["RavenDb:Url"] };
    options.Database = configuration["RavenDb:Database"];
    options.Certificate = LoadCertificate();
    
    options.ConfigureConventions = conventions =>
    {
        conventions.MaxNumberOfRequestsPerSession = 30;
        conventions.UseOptimisticConcurrency = true;
    };
});
```

### Testing

```csharp
[Fact]
public async Task CreateProduct_ShouldSaveEvents()
{
    // Arrange
    using var testDriver = new RavenTestDriver();
    var store = testDriver.GetDocumentStore();
    var eventStore = new RavenDbEventStore(store, new JsonEventSerializer());
    
    var product = Product.Create("Laptop", 1299.99m, "Electronics");
    
    // Act
    await eventStore.SaveEventsAsync(product.Id, product.GetUncommittedEvents(), 0);
    
    // Assert
    var events = await eventStore.GetEventsAsync(product.Id);
    Assert.Single(events);
}
```

## 📈 Performance Characteristics

| Operation | EF Core | RavenDB | Notes |
|-----------|---------|---------|-------|
| Append Event | O(1) | O(1) | Both excellent |
| Load Aggregate | O(n) | O(n) | n = event count |
| Check Version | O(1) | O(1) | Map-Reduce vs SQL index |
| Full-text Search | ⚠️ Complex | ✅ Built-in | RavenDB advantage |
| Distributed Storage | ❌ Complex | ✅ Native | RavenDB advantage |

## 🔐 Concurrency Strategy

RavenDB uses ETags for optimistic concurrency:

```csharp
// Enable optimistic concurrency
session.Advanced.UseOptimisticConcurrency = true;

try
{
    await session.SaveChangesAsync();
}
catch (Raven.Client.Exceptions.ConcurrencyException)
{
    // Document was modified by another request
    // ETag mismatch detected
}
```

## 🚀 Production Deployment

### Docker Compose

```yaml
version: '3.8'
services:
  ravendb:
    image: ravendb/ravendb:latest
    ports:
      - "8080:8080"
      - "38888:38888"
    environment:
      - RAVEN_ARGS=--Setup.Mode=None
      - RAVEN_Security_UnsecuredAccessAllowed=PublicNetwork
    volumes:
      - ravendb-data:/opt/RavenDB/Server/RavenData
```

### Configuration

```json
{
  "RavenDb": {
    "Urls": ["http://localhost:8080"],
    "Database": "EventStore",
    "Certificate": null
  }
}
```

## 🎓 Key Learnings

1. **Same Domain, Different Persistence**: The domain layer is 100% reusable
2. **Map-Reduce Power**: Efficient aggregations without complex SQL
3. **Schemaless Flexibility**: Event schema can evolve without migrations
4. **Document Model**: Natural fit for event sourcing
5. **Built-in Features**: Versioning, full-text search, distributed storage

## 🔄 Migration Path

**From EF Core to RavenDB:**
1. Keep domain layer unchanged
2. Replace infrastructure layer
3. Update service configuration
4. Migrate data (export/import)

**From RavenDB to EF Core:**
1. Keep domain layer unchanged
2. Replace infrastructure layer
3. Update service configuration
4. Migrate data (export/import)

## 📚 Additional Resources

- **RavenDB Documentation**: https://ravendb.net/docs
- **Comparison Guide**: See COMPARISON.md
- **EF Core Version**: For reference implementation

## 🎯 When to Use This Version

✅ **Perfect for:**
- Microservices architectures
- Cloud-native applications
- Rapid development cycles
- Schema evolution requirements
- Distributed systems
- NoSQL preferences

⚠️ **Consider EF Core instead if:**
- Existing SQL infrastructure
- Strong SQL expertise in team
- Complex relational queries needed
- Regulatory SQL requirements

## 🏆 Summary

This RavenDB implementation demonstrates:
- Clean architecture principles
- Persistence ignorance in domain layer
- Power of abstraction and interfaces
- SOLID principles in practice
- Modern document database usage
- Production-ready event sourcing

The fact that **only the persistence layer** needed to change proves the quality of the architecture!
