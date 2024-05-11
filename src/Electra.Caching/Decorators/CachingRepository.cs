using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Foundatio.Caching;
using Electra.Common.Caching.Extensions;
using Electra.Persistence;
using Electra.Models.Entities;

namespace Electra.Common.Caching.Decorators;

public abstract record DbCacheResult<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;    
}
public sealed record DbCacheResult<T> : DbCacheResult<T, Guid> where T : IEntity<Guid>, new() { }

// Todo - Consider not inheriting from IGenericRepository for the cache repository and change return values to DbCacheResult
public interface ICachingRepositoryDecorator<T, TKey> : IGenericRepository<T, TKey>
    where T : IEntity<TKey>, new() where TKey : IEquatable<TKey>
{
    T Insert(CacheEntry<T> entry);
    T Update(CacheEntry<T> entry);
    T Upsert(CacheEntry<T> entry);
    T Insert([NotNull] T entity, CacheOptions opts = default);
    T Update([NotNull] T entity, CacheOptions opts = default);
    T Upsert([NotNull] T entity, CacheOptions opts = default);

    Task<T> InsertAsync(CacheEntry<T> entry);
    Task<T> UpdateAsync(CacheEntry<T> entry);
    Task<T> UpsertAsync(CacheEntry<T> entry);
    Task<T> InsertAsync([NotNull] T entity, CacheOptions opts = default);
    Task<T> UpdateAsync([NotNull] T entity, CacheOptions opts = default);
    Task<T> UpsertAsync([NotNull] T entity, CacheOptions opts = default);
}

public interface ICachingRepositoryDecorator<T> : ICachingRepositoryDecorator<T, Guid>, IGenericRepository<T> where T : IEntity<Guid>, new() { }


public class CachingRepository<T>(
    ICacheClient cache,
    IGenericRepository<T, Guid> db,
    ILogger<CachingRepository<T, Guid>> log)
    : CachingRepository<T, Guid>(cache, db, log)
    where T : IEntity<Guid>, new();

public class CachingRepository<T, TKey>(
    ICacheClient cache,
    IGenericRepository<T, TKey> db,
    ILogger<CachingRepository<T, TKey>> log)
    : ICachingRepositoryDecorator<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected readonly IGenericRepository<T,TKey> db = db;
    protected readonly ICacheClient cache = cache;
    protected readonly ILogger<CachingRepository<T, TKey>> log = log;
    protected readonly string type = typeof(T).Name; // todo - make sure Type().Name is suffice for cache key
    protected readonly TimeSpan defaultExpiration = TimeSpan.FromMinutes(15);
    protected readonly string prefix = $"db_{typeof(T).Name}";  // todo - pull the cache-key prefix in from appSettings.json
    protected readonly CacheOptions defaultOptions = new();

    public IEnumerable<T> GetAll() => GetAllAsync().GetAwaiter().GetResult();
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var key = $"{prefix}_all";
        var success = await cache.TryGetItemAsync(key, out IEnumerable<T> results);
        
        if(!success)
        {
            log.LogInformation($"cache miss for {key}");
            results = await db.GetAllAsync();
            
            if(results != null)
            {
                var res = await cache.AddCollectionAsync(prefix, results, defaultOptions);
                log.LogInformation($"cached {res} items");
            }
        }
        
        log.LogInformation($"cache hit for {key}");
        
        return results;
    }
    
    public T FindById(TKey id) => FindByIdAsync(id).GetAwaiter().GetResult();

    public async Task<T> FindByIdAsync(TKey id)
    {
        var key = $"{prefix}_{id}";
        var success = await cache.TryGetItemAsync(key, out T results);
        
        if(!success)
        {
            log.LogInformation($"cache miss for {key}");
            results = await db.FindByIdAsync(id);

            if(results != null)
            {
                var res = await cache.AddAsync(key, results, defaultOptions.Expiry);
            }
        }
        
        log.LogInformation($"cache hit for {prefix}");
        
        return results;
    }

    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => FindAsync(predicate).GetAwaiter().GetResult();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var key = $"{prefix}_find";
        var success = await cache.TryGetItemAsync(key, out IEnumerable<T> results);
        
        if(!success)
        {
            log.LogInformation($"cache miss for {key}");
            results = await db.FindAsync(predicate);
            if(results != null)
            {
                var res = await cache.AddCollectionAsync(prefix, results, defaultOptions);
                log.LogInformation($"cached {res} items");
            }
        }
        
        log.LogInformation($"cache hit for {key}");
        
        return results;
    }

    public T Insert(T entity) => InsertAsync(entity).GetAwaiter().GetResult();

    public T Update(T entity) => UpdateAsync(entity).GetAwaiter().GetResult();

    public T Upsert(T entity) => UpsertAsync(entity).GetAwaiter().GetResult();

    public void Delete(TKey id) => DeleteAsync(id).GetAwaiter().GetResult();

    public void Delete(T entity) => DeleteAsync(entity).GetAwaiter().GetResult();

    public async Task<T> InsertAsync([NotNull] T entity)
    {
        var dbRes = await db.InsertAsync(entity);
        
        var res = await cache.SetAsync(entity.Id.ToString(), entity, defaultExpiration);
        if(!res)
            log.LogInformation($"failed to cache item w/ id: {entity.Id}");

        log.LogInformation($"cached item with id: {entity.Id}");
        
        return dbRes;
    }

    public async Task<T> UpdateAsync([NotNull] T entity)
    {
        var dbRes = await db.UpdateAsync(entity);
        
        var res = await cache.SetAsync(entity.Id.ToString(), entity, defaultExpiration);
        if(!res)
            log.LogInformation($"failed to set cache for item w/ id: {entity.Id}");

        log.LogInformation($"cached item with id: {entity.Id}");
        
        return dbRes;
    }

    public async Task<T> UpsertAsync([NotNull] T entity)
    {
        var dbRes = await db.UpsertAsync(entity);
        
        var res = await cache.SetAsync(entity.Id.ToString(), entity, defaultExpiration);
        if(!res)
            log.LogInformation($"failed to set cache for item w/ id: {entity.Id}");

        log.LogInformation($"cached item with id: {entity.Id}");
        
        return dbRes;
    }

    public async Task DeleteAsync([NotNull] TKey id)
    {
        log.LogInformation($"removing item with id: {id} from cache");
        var res = await cache.RemoveAsync(id.ToString());

        await db.DeleteAsync(id);
    }

    public async Task DeleteAsync([NotNull] T entity) => await DeleteAsync(entity.Id);

    public T Insert(CacheEntry<T> entry) => InsertAsync(entry).GetAwaiter().GetResult();

    public T Update(CacheEntry<T> entry) => UpdateAsync(entry).GetAwaiter().GetResult();

    public T Upsert(CacheEntry<T> entry) => UpsertAsync(entry).GetAwaiter().GetResult();

    public T Insert([NotNull] T entity, CacheOptions opts = default) =>
        InsertAsync(entity, opts).GetAwaiter().GetResult();

    public T Update([NotNull] T entity, CacheOptions opts = default) =>
        UpdateAsync(entity, opts).GetAwaiter().GetResult();

    public T Upsert([NotNull] T entity, CacheOptions opts = default) =>
        UpdateAsync(entity, opts).GetAwaiter().GetResult();

    public async Task<T> InsertAsync(CacheEntry<T> entry)
    {
        var dbRes = await db.InsertAsync(entry.Value);
        
        if(dbRes != null)
        {
            var res = await cache.AddAsync(entry.Key, entry.Value, entry.Options.Expiry);
            if(!res)
                log.LogWarning($"(inserting to cache failed for id: {entry.Key}");
        }
        
        return dbRes;
    }

    public async Task<T> UpdateAsync(CacheEntry<T> entry)
    {
        var dbRes = await db.UpdateAsync(entry.Value);
        
        if(dbRes != null)
        {
            var res = await cache.SetAsync(entry.Key, entry.Value, entry.Options.Expiry);
             if(!res)
                log.LogWarning($"(inserting to cache failed for id: {entry.Key}");
        }
        
        return dbRes;
    }

    public async Task<T> UpsertAsync(CacheEntry<T> entry)
    {
        var dbRes = await db.UpsertAsync(entry.Value);
        
        if(dbRes != null)
        {
            var res = await cache.SetAsync(entry.Key, entry.Value, entry.Options.Expiry);
             if(!res)
                log.LogWarning($"(inserting to cache failed for id: {entry.Key}");
        }
        
        return dbRes;
    }

    public async Task<T> InsertAsync([NotNull] T entity, CacheOptions opts = default)
        => await InsertAsync(new CacheEntry<T>() { Key = entity.Id.ToString(), Value = entity});

    public async Task<T> UpdateAsync([NotNull] T entity, CacheOptions opts = default)
        => await UpdateAsync(new CacheEntry<T>() { Key = entity.Id.ToString(), Value = entity});


    public async Task<T> UpsertAsync([NotNull] T entity, CacheOptions opts = default)
        => await UpsertAsync(new CacheEntry<T>() { Key = entity.Id.ToString(), Value = entity});

}