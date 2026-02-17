# Aero.Persistence.Core

Core persistence abstractions and interfaces for the Aero framework.

## Overview

`Aero.Persistence.Core` defines the fundamental contracts and abstractions for data persistence in the Aero framework. It provides the base interfaces that all concrete persistence implementations (Entity Framework, RavenDB, Marten, etc.) must implement.

## Key Components

### Repository Interfaces

The core repository contracts that define data access operations:

#### IGenericRepository<T, TKey>

The primary interface for entity repositories:

```csharp
public interface IGenericRepository<T, TKey> 
    : IReadOnlyRepository<T, TKey>, IWriteOnlyRepository<T, TKey>
    where T : IEntity<TKey>, new() 
    where TKey : IEquatable<TKey>
{
    // Combines read and write operations
}
```

#### IReadOnlyRepository<T, TKey>

Interface for read-only data access:

```csharp
public interface IReadOnlyRepository<T, TKey> 
    : IReadonlyRepositorySync<T, TKey>, IReadonlyRepositoryAsync<T, TKey>
    where T : IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
}

public interface IReadonlyRepositoryAsync<T, TKey> 
    where T : IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> FindByIdAsync(TKey id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}
```

#### IWriteOnlyRepository<T, TKey>

Interface for write operations:

```csharp
public interface IWriteOnlyRepository<T, TKey> 
    : IWriteOnlyRepositorySync<T, TKey>, IWriteOnlyRepositoryAsync<T, TKey>
    where T : IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
}

public interface IWriteOnlyRepositoryAsync<T, TKey> 
    where T : IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    Task<T> InsertAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<T> UpsertAsync(T entity);
    Task DeleteAsync(TKey id);
    Task DeleteAsync(T entity);
}
```

### User Repository

Specialized interface for user-related operations:

```csharp
public interface IUserRepository<TUser> where TUser : class
{
    Task<TUser> FindByEmailAsync(string email);
    Task<TUser> FindByUsernameAsync(string username);
    Task<bool> CheckPasswordAsync(TUser user, string password);
    Task<IdentityResult> CreateAsync(TUser user, string password);
    Task<IdentityResult> UpdateAsync(TUser user);
    Task<IdentityResult> DeleteAsync(TUser user);
}
```

## Design Principles

### Async-First Design

All data access operations are designed to be asynchronous:

```csharp
// Always async
Task<T> GetByIdAsync(TKey id);

// Sync wrappers provided for compatibility
T GetById(TKey id) => GetByIdAsync(id).GetAwaiter().GetResult();
```

### Generic Key Support

Support for multiple key types:

```csharp
// String keys (default for document databases)
public interface IGenericRepository<T> : IGenericRepository<T, string> 
    where T : IEntity<string>, new()
{
}

// Guid keys
public class OrderRepository : IGenericRepository<Order, Guid>

// Integer keys  
public class LegacyProductRepository : IGenericRepository<Product, int>
```

### Expression-Based Queries

Type-safe querying using LINQ expressions:

```csharp
// Find with predicate
var activeProducts = await repository.FindAsync(p => p.IsActive && p.Price > 0);

// Complex queries
var recentOrders = await repository.FindAsync(o => 
    o.OrderDate > DateTime.Now.AddDays(-30) && 
    o.Total > 100);
```

## Implementation Guidelines

### Creating a Custom Repository Interface

```csharp
// Define specific interface
public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm);
    Task UpdateInventoryAsync(string productId, int quantity);
}

// Implement for specific database
public class ProductEfRepository : GenericEntityFrameworkRepository<Product>, IProductRepository
{
    public async Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId)
    {
        return await DbContext.Products
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync();
    }
    
    // ... other implementations
}
```

### Repository Registration

```csharp
// Register generic repository
services.AddScoped(typeof(IGenericRepository<>), typeof(GenericEfRepository<>));

// Register specific implementation
services.AddScoped<IProductRepository, ProductEfRepository>();

// Register with decorator
services.AddScoped<IGenericRepository<Product>, GenericEfRepository<Product>>();
services.Decorate<IGenericRepository<Product>, CachingRepository<Product>>();
```

## Database-Specific Interfaces

Each persistence implementation extends the core interfaces:

### Entity Framework

```csharp
public interface IGenericEntityFrameworkRepository<T, TKey> 
    : IGenericRepository<T, TKey>
    where T : class, IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    DbSet<T> DbSet { get; }
    IQueryable<T> Queryable { get; }
    Task<int> SaveChangesAsync();
}
```

### RavenDB

```csharp
public interface IRavenDbRepository<T> : IGenericRepository<T, string> 
    where T : Entity
{
    IAsyncDocumentSession Session { get; }
    Task<T> LoadAsync(string id);
    Task StoreAsync(T entity);
}
```

### Marten

```csharp
public interface IGenericMartenRepository<T, TKey> 
    : IGenericRepository<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    IDocumentSession Session { get; }
    IQueryable<T> Query();
}
```

## Transaction Support

```csharp
// Unit of Work pattern
public interface IUnitOfWork : IDisposable
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

// Usage
using (var uow = _unitOfWorkFactory.Create())
{
    await uow.BeginTransactionAsync();
    
    try
    {
        await _orderRepository.InsertAsync(order);
        await _inventoryRepository.UpdateAsync(inventory);
        
        await uow.CommitAsync();
    }
    catch
    {
        await uow.RollbackAsync();
        throw;
    }
}
```

## Best Practices

1. **Program to Interfaces** - Always depend on `IGenericRepository<T>` not concrete implementations
2. **Use Specific Interfaces** - Create domain-specific repository interfaces when needed
3. **Avoid Repository in Domain** - Keep domain logic pure, use repositories only at application boundary
4. **Leverage Async** - Always use async methods for I/O operations
5. **Decorate for Cross-Cutting** - Use CachingRepository, LoggingRepository decorators

## Related Packages

- `Aero.Core` - Entity definitions
- `Aero.Persistence` - Base repository implementations
- `Aero.EfCore` - Entity Framework implementation
- `Aero.RavenDB` - RavenDB implementation
- `Aero.Marten` - Marten implementation
- `Aero.Caching` - Caching decorators
