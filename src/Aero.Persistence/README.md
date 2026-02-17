# Aero.Persistence

Base repository implementations and persistence utilities for the Aero framework.

## Overview

`Aero.Persistence` provides concrete base implementations of the repository interfaces defined in `Aero.Persistence.Core`. It includes generic repository implementations that serve as the foundation for database-specific implementations.

## Key Components

### GenericRepository<T, TKey>

The abstract base class for all repository implementations:

```csharp
public abstract class GenericRepository<T, TKey> : IGenericRepository<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected readonly ILogger log;

    // Sync wrappers (call async implementations)
    public virtual IEnumerable<T> GetAll() => GetAllAsync().GetAwaiter().GetResult();
    public virtual T FindById(TKey id) => FindByIdAsync(id).GetAwaiter().GetResult();
    public virtual T Insert(T entity) => InsertAsync(entity).GetAwaiter().GetResult();
    
    // Abstract async methods (must be implemented)
    public abstract Task<IEnumerable<T>> GetAllAsync();
    public abstract Task<T> FindByIdAsync(TKey id);
    public abstract Task<T> InsertAsync(T entity);
    public abstract Task<long> CountAsync();
    public abstract Task<bool> ExistsAsync(TKey id);
    
    // ... additional methods
}
```

### LiteDB Repository

Embedded database implementation:

```csharp
public class LiteDbRepository<T> : GenericRepository<T, string> 
    where T : Entity<string>, new()
{
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<T> _collection;

    public LiteDbRepository(ILiteDatabase database, ILogger<LiteDbRepository<T>> log) 
        : base(log)
    {
        _database = database;
        _collection = database.GetCollection<T>();
    }

    public override async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Task.FromResult(_collection.FindAll());
    }

    // ... implementation of other methods
}
```

## Implementation Patterns

### Creating a Database-Specific Implementation

```csharp
public class PostgreSqlRepository<T> : GenericRepository<T, string> 
    where T : Entity, new()
{
    private readonly NpgsqlConnection _connection;

    public PostgreSqlRepository(NpgsqlConnection connection, ILogger<PostgreSqlRepository<T>> log) 
        : base(log)
    {
        _connection = connection;
    }

    public override async Task<IEnumerable<T>> GetAllAsync()
    {
        // PostgreSQL-specific implementation
        var sql = $"SELECT * FROM {typeof(T).Name}s";
        return await _connection.QueryAsync<T>(sql);
    }

    public override async Task<T> FindByIdAsync(string id)
    {
        var sql = $"SELECT * FROM {typeof(T).Name}s WHERE Id = @id";
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { id });
    }

    public override async Task<T> InsertAsync(T entity)
    {
        entity.Id = entity.Id ?? Guid.NewGuid().ToString();
        entity.CreatedOn = DateTimeOffset.UtcNow;
        
        // Insert implementation
        return entity;
    }

    // ... implement remaining abstract methods
}
```

### Repository Decorators

Decorators that wrap repository implementations:

#### CachingRepository

```csharp
public class CachingRepository<T, TKey> : GenericRepository<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    private readonly IGenericRepository<T, TKey> _inner;
    private readonly IFusionCache _cache;

    public CachingRepository(
        IGenericRepository<T, TKey> inner, 
        IFusionCache cache,
        ILogger<CachingRepository<T, TKey>> log) : base(log)
    {
        _inner = inner;
        _cache = cache;
    }

    public override async Task<T> FindByIdAsync(TKey id)
    {
        var cacheKey = $"{typeof(T).Name}:{id}";
        
        return await _cache.GetOrSetAsync(cacheKey, 
            async _ => await _inner.FindByIdAsync(id),
            TimeSpan.FromMinutes(10));
    }

    public override async Task<T> InsertAsync(T entity)
    {
        var result = await _inner.InsertAsync(entity);
        await InvalidateCacheAsync(entity.Id);
        return result;
    }

    private async Task InvalidateCacheAsync(TKey id)
    {
        await _cache.RemoveAsync($"{typeof(T).Name}:{id}");
    }

    // ... delegate other methods to _inner
}
```

## Registration Patterns

### Basic Registration

```csharp
// Register concrete implementation
services.AddScoped<IGenericRepository<Product>, ProductRepository>();

// Register generic implementation
services.AddScoped(typeof(IGenericRepository<>), typeof(GenericEfRepository<>));
```

### Decorator Chain

```csharp
// Base implementation
services.AddScoped<IGenericRepository<Product>, ProductRepository>();

// Add decorators (order matters - first decorator wraps the base)
services.Decorate<IGenericRepository<Product>, CachingRepository<Product, string>>();
services.Decorate<IGenericRepository<Product>, LoggingRepository<Product, string>>();
services.Decorate<IGenericRepository<Product>, TimingRepository<Product, string>>();
```

### Named/Keyed Registration

```csharp
// Different implementations for different contexts
services.AddScoped<IGenericRepository<AuditLog>, AuditLogEfRepository>("sql");
services.AddScoped<IGenericRepository<AuditLog>, AuditLogElasticRepository>("elastic");

// Resolve by key
var sqlRepo = serviceProvider.GetKeyedService<IGenericRepository<AuditLog>>("sql");
```

## Repository Factory

```csharp
public interface IRepositoryFactory
{
    IGenericRepository<T> Create<T>() where T : Entity, new();
    IGenericRepository<T, TKey> Create<T, TKey>() where T : IEntity<TKey>, new();
}

public class RepositoryFactory : IRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;

    public RepositoryFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IGenericRepository<T> Create<T>() where T : Entity, new()
    {
        return _serviceProvider.GetRequiredService<IGenericRepository<T>>();
    }

    public IGenericRepository<T, TKey> Create<T, TKey>() where T : IEntity<TKey>, new()
    {
        return _serviceProvider.GetRequiredService<IGenericRepository<T, TKey>>();
    }
}
```

## Transaction Management

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextTransaction _transaction;

    public UnitOfWork(DbContext context)
    {
        _transaction = context.Database.BeginTransaction();
    }

    public async Task CommitAsync()
    {
        await _transaction.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}
```

## Best Practices

1. **Inherit from GenericRepository** - Provides sync wrappers and common functionality
2. **Implement All Abstract Methods** - Required for compilation
3. **Handle Audit Fields** - Set CreatedOn, ModifiedOn automatically
4. **Use Parameterized Queries** - Prevent SQL injection
5. **Dispose Resources Properly** - Implement IDisposable when needed
6. **Log Operations** - Use injected ILogger for debugging

## Related Packages

- `Aero.Persistence.Core` - Repository interfaces
- `Aero.Core` - Entity definitions
- `Aero.Caching` - Caching decorators
- `Aero.EfCore` - Entity Framework implementation
- `Aero.RavenDB` - RavenDB implementation
- `Aero.Marten` - Marten implementation
