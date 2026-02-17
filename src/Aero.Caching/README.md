# Aero.Caching

FusionCache-based distributed caching with Redis backplane for the Aero framework.

## Overview

`Aero.Caching` provides a robust caching layer built on ZiggyCreatures.FusionCache with Redis backplane support. It offers transparent caching decorators for repositories and supports both in-memory and distributed caching scenarios.

## Key Components

### CachingRepository<T, TKey>

Transparent caching decorator for repositories:

```csharp
public class CachingRepository<T, TKey> : GenericRepository<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    private readonly IGenericRepository<T, TKey> _inner;
    private readonly IFusionCache _cache;
    private readonly ILogger<CachingRepository<T, TKey>> _log;

    public CachingRepository(
        IGenericRepository<T, TKey> inner,
        IFusionCache cache,
        ILogger<CachingRepository<T, TKey>> log) : base(log)
    {
        _inner = inner;
        _cache = cache;
        _log = log;
    }

    public override async Task<T> FindByIdAsync(TKey id)
    {
        var cacheKey = GetCacheKey(id);
        
        return await _cache.GetOrSetAsync(cacheKey,
            async _ => await _inner.FindByIdAsync(id),
            new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.High
            });
    }

    public override async Task<T> InsertAsync(T entity)
    {
        var result = await _inner.InsertAsync(entity);
        await InvalidateCacheAsync(result.Id);
        return result;
    }

    public override async Task<T> UpdateAsync(T entity)
    {
        var result = await _inner.UpdateAsync(entity);
        await InvalidateCacheAsync(result.Id);
        return result;
    }

    public override async Task DeleteAsync(TKey id)
    {
        await _inner.DeleteAsync(id);
        await InvalidateCacheAsync(id);
    }

    private string GetCacheKey(TKey id) => $"{typeof(T).Name}:{id}";
    
    private async Task InvalidateCacheAsync(TKey id)
    {
        await _cache.RemoveAsync(GetCacheKey(id));
        _log.LogDebug("Cache invalidated for {Type}:{Id}", typeof(T).Name, id);
    }
}
```

## Setup

### Configuration

```csharp
// appsettings.json
{
  "Caching": {
    "Redis": {
      "ConnectionString": "localhost:6379",
      "InstanceName": "AeroCache"
    },
    "DefaultDurationMinutes": 10,
    "BackplaneEnabled": true
  }
}

// Program.cs
builder.Services.AddFusionCache()
    .WithDefaultEntryOptions(new FusionCacheEntryOptions
    {
        Duration = TimeSpan.FromMinutes(10),
        Priority = CacheItemPriority.Normal,
        AllowBackgroundDistributedCacheOperations = true
    })
    .WithDistributedCache(
        new RedisCache(new RedisCacheOptions
        {
            Configuration = builder.Configuration["Caching:Redis:ConnectionString"],
            InstanceName = builder.Configuration["Caching:Redis:InstanceName"]
        })
    )
    .WithBackplane(
        new RedisBackplane(new RedisBackplaneOptions
        {
            Configuration = builder.Configuration["Caching:Redis:ConnectionString"]
        })
    )
    .AsHybridCache();

// Register caching decorator
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericEfRepository<>));
builder.Services.Decorate(typeof(IGenericRepository<>), typeof(CachingRepository<,>));
```

### Fusion Cache Configuration

```csharp
public static class CachingExtensions
{
    public static IServiceCollection AddAeroCaching(
        this IServiceCollection services, 
        IConfiguration config)
    {
        var redisConnection = config["Caching:Redis:ConnectionString"];
        
        services.AddFusionCache()
            .WithDefaultEntryOptions(new FusionCacheEntryOptions
            {
                // L1 (memory) cache duration
                Duration = TimeSpan.FromMinutes(5),
                
                // L2 (distributed) cache duration
                DistributedCacheDuration = TimeSpan.FromMinutes(30),
                
                // Enable fail-safe mode
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(2),
                
                // Background factory timeouts
                FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
                FactoryHardTimeout = TimeSpan.FromSeconds(1),
                
                // Circuit breaker for distributed cache
                DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2)
            })
            .WithDistributedCache(
                new RedisCache(new RedisCacheOptions
                {
                    Configuration = redisConnection
                })
            )
            .WithBackplane(
                new RedisBackplane(new RedisBackplaneOptions
                {
                    Configuration = redisConnection
                })
            );

        return services;
    }
}
```

## Advanced Features

### Cache Entry Options

```csharp
// Default options
var defaultOptions = new FusionCacheEntryOptions
{
    Duration = TimeSpan.FromMinutes(10),
    Priority = CacheItemPriority.High,
    
    // Fail-safe configuration
    IsFailSafeEnabled = true,
    FailSafeMaxDuration = TimeSpan.FromHours(1),
    
    // Eager refresh (refresh before expiration)
    EagerRefreshThreshold = 0.8f,
    
    // Factory timeouts
    FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
    FactoryHardTimeout = TimeSpan.FromSeconds(2),
    
    // Distributed cache options
    DistributedCacheDuration = TimeSpan.FromMinutes(30),
    AllowBackgroundDistributedCacheOperations = true,
    
    // JIT (Just-In-Time) refreshes
    JitterMaxDuration = TimeSpan.FromSeconds(10)
};

// Per-entry options
var result = await cache.GetOrSetAsync(key,
    async _ => await LoadDataAsync(),
    new FusionCacheEntryOptions
    {
        Duration = TimeSpan.FromMinutes(5),
        Tags = new[] { "products", "featured" }
    });
```

### Tag-Based Invalidation

```csharp
// Set cache entry with tags
await cache.SetAsync("product-1", product, new FusionCacheEntryOptions
{
    Tags = new[] { "products", "category-electronics" }
});

// Invalidate by tag
await cache.RemoveByTagAsync("category-electronics");

// Invalidate multiple tags
await cache.RemoveByTagAsync(new[] { "products", "featured" });
```

### Conditional Caching

```csharp
// Cache only if condition is met
var result = await cache.GetOrSetAsync(key,
    async _ => await LoadDataAsync(),
    options,
    skipDistributedCacheCondition: (_, _) => result.IsVolatile);

// Skip memory cache for large objects
var largeData = await cache.GetOrSetAsync(key,
    async _ => await LoadLargeDataAsync(),
    new FusionCacheEntryOptions
    {
        SkipMemoryCache = true, // Only use distributed cache
        DistributedCacheDuration = TimeSpan.FromHours(1)
    });
```

### Events and Logging

```csharp
// Configure event handlers
services.AddFusionCache()
    .SetupRedisBackplane()
    .WithOptions(options =>
    {
        options.DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2);
    })
    .AddFusionCacheInstrumentation(); // OpenTelemetry metrics

// Subscribe to events
cache.Events.Memory.OnSet += (sender, e) =>
{
    logger.LogDebug("Memory cache set: {Key}", e.Key);
};

cache.Events.Distributed.OnRemove += (sender, e) =>
{
    logger.LogDebug("Distributed cache removed: {Key}", e.Key);
};

cache.Events.Backplane.OnMessageReceived += (sender, e) =>
{
    logger.LogDebug("Backplane message received: {Message}", e.Message);
};
```

### Circuit Breaker

```csharp
services.AddFusionCache()
    .WithDefaultEntryOptions(new FusionCacheEntryOptions
    {
        // Circuit breaker for distributed cache failures
        DistributedCacheCircuitBreakerDuration = TimeSpan.FromSeconds(2),
        
        // What to do when circuit is open
        AllowBackgroundDistributedCacheOperations = true
    });
```

## Repository Caching Patterns

### Decorator Pattern

```csharp
// Base registration
services.AddScoped<IProductRepository, ProductRepository>();

// Add caching decorator
services.Decorate<IProductRepository, CachingProductRepository>();

// Caching implementation
public class CachingProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly IFusionCache _cache;

    public async Task<Product> GetByIdAsync(string id)
    {
        return await _cache.GetOrSetAsync(
            $"product:{id}",
            async _ => await _inner.GetByIdAsync(id),
            options: new() { Duration = TimeSpan.FromMinutes(10) });
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId)
    {
        return await _cache.GetOrSetAsync(
            $"products:category:{categoryId}",
            async _ => await _inner.GetByCategoryAsync(categoryId),
            options: new() { Duration = TimeSpan.FromMinutes(5) });
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        var result = await _inner.UpdateAsync(product);
        await InvalidateProductCacheAsync(result.Id);
        return result;
    }

    private async Task InvalidateProductCacheAsync(string id)
    {
        await _cache.RemoveAsync($"product:{id}");
        await _cache.RemoveByTagAsync($"product:{id}:related");
    }
}
```

### Cache-Aside Pattern

```csharp
public async Task<Product> GetProductAsync(string id)
{
    // Try cache first
    var cached = await _cache.TryGetAsync<Product>($"product:{id}");
    if (cached.HasValue)
        return cached.Value;

    // Load from database
    var product = await _repository.FindByIdAsync(id);
    
    if (product != null)
    {
        // Store in cache
        await _cache.SetAsync(
            $"product:{id}",
            product,
            new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(10) });
    }

    return product;
}
```

### Read-Through Pattern

```csharp
public async Task<Product> GetProductAsync(string id)
{
    // FusionCache handles cache miss automatically
    return await _cache.GetOrSetAsync(
        $"product:{id}",
        async _ => await _repository.FindByIdAsync(id),
        new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(10),
            EagerRefreshThreshold = 0.8f, // Refresh at 80% of TTL
            IsFailSafeEnabled = true
        });
}
```

## Best Practices

1. **Set Appropriate TTLs** - Balance between freshness and performance
2. **Use Tags** - Enable efficient invalidation of related data
3. **Enable Fail-Safe** - Serve stale data rather than fail
4. **Use Eager Refresh** - Prevent cache stampedes
5. **Monitor Cache Metrics** - Track hit rates and performance
6. **Invalidate on Writes** - Always clear cache when data changes
7. **Use Circuit Breaker** - Handle Redis failures gracefully

## Related Packages

- `Aero.Persistence` - Repository implementations to decorate
- `Aero.Persistence.Core` - Repository interfaces
- `Aero.Common` - Decorator base classes
