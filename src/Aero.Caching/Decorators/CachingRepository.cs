using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Aero.Core.Entities;
using Aero.EfCore.Data;

namespace Aero.Caching.Decorators;

public abstract record DbCacheResult<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;    
}
public sealed record DbCacheResult<T> : DbCacheResult<T, string> where T : IEntity<string>, new() { }

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

public interface ICachingRepositoryDecorator<T>
    : ICachingRepositoryDecorator<T, string>, IGenericRepository<T>
    where T : IEntity<string>, new();


public class CachingRepository<T>(
    ICacheService cache,
    IGenericRepository<T, string> db,
    ILogger<CachingRepository<T, string>> log)
    : CachingRepository<T, string>(cache, db, log)
    where T : IEntity<string>, new();

public class CachingRepository<T, TKey>(
    ICacheService cache,
    IGenericRepository<T, TKey> db,
    ILogger<CachingRepository<T, TKey>> log)
    : ICachingRepositoryDecorator<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected readonly IGenericRepository<T,TKey> db = db;
    protected readonly ICacheService cache = cache;
    protected readonly ILogger<CachingRepository<T, TKey>> log = log;
    protected readonly string type = typeof(T).Name; // todo - make sure Type().Name is suffice for cache key
    protected readonly TimeSpan defaultExpiration = TimeSpan.FromMinutes(15);
    protected readonly string prefix = $"db_{typeof(T).Name}";  // todo - pull the cache-key prefix in from appSettings.json
    protected readonly CacheOptions defaultOptions = new();

    public IEnumerable<T> GetAll() => GetAllAsync().GetAwaiter().GetResult();
    
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var key = $"{prefix}_all";
        var success = await cache.GetAsync<T>(key);
        
        if(success.IsNone)
        {
            log.LogInformation($"cache miss for {key}");
            var results = await db.GetAllAsync();
            
            if(results != null)
            {
                await cache.SetAsync(prefix, results, defaultOptions.Expiry);
                log.LogInformation($"cached {results.Count()} items");
                return results;
            }

            return [];
        }
        else
        {
            log.LogInformation($"cache hit for {key}");
            return success.AsEnumerable();
        }
    }
    
    public T FindById(TKey id) => FindByIdAsync(id).GetAwaiter().GetResult();

    public async Task<T> FindByIdAsync(TKey id)
    {
        var key = $"{prefix}_{id}";
        var success = await cache.GetAsync<T>(key);

        var ret = success.Match(Some: x => x, None: () => default);
        
        if(success.IsNone)
        {
            log.LogInformation($"cache miss for {key}");
            var results = await db.FindByIdAsync(id);

            if(results != null)
                await cache.SetAsync(key, results);

            return ret;
        }
        else
        {
            log.LogInformation("cache hit for {key}", key);
            return ret;
        }
        
        log.LogInformation($"cache hit for {prefix}");
        
        return ret;
    }

    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => FindAsync(predicate).GetAwaiter().GetResult();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var key = $"{prefix}_find";
        var success = await cache.GetAsync<T>(key);
        
        if(success.IsNone)
        {
            log.LogInformation($"cache miss for {key}");
            var results = await db.FindAsync(predicate);
            if(results != null && results.Any())
            {
                await cache.SetAsync<T>(prefix, results);
                log.LogInformation($"cached {results.Count()} items");
                return results;
            }

            return [];
        }
        else
        {
            log.LogInformation("cache hit for {key}", key);
            return success.AsEnumerable();
        }
    }

    public T Insert(T entity) => InsertAsync(entity).GetAwaiter().GetResult();

    public T Update(T entity) => UpdateAsync(entity).GetAwaiter().GetResult();

    public T Upsert(T entity) => UpsertAsync(entity).GetAwaiter().GetResult();

    public void Delete(TKey id) => DeleteAsync(id).GetAwaiter().GetResult();

    public void Delete(T entity) => DeleteAsync(entity).GetAwaiter().GetResult();

    public async Task<T> InsertAsync([NotNull] T entity)
    {
        var dbRes = await db.InsertAsync(entity);
        
        await cache.SetAsync(entity.Id.ToString(), entity, defaultExpiration);
        
        return dbRes;
    }

    public async Task<T> UpdateAsync([NotNull] T entity)
    {
        var dbRes = await db.UpdateAsync(entity);
        
        await cache.SetAsync(entity.Id.ToString(), entity, defaultExpiration);


        log.LogInformation($"cached item with id: {entity.Id}");
        
        return dbRes;
    }

    public async Task<T> UpsertAsync([NotNull] T entity)
    {
        var dbRes = await db.UpsertAsync(entity);
        
        await cache.SetAsync(entity.Id.ToString(), entity, defaultExpiration);

        log.LogInformation($"cached item with id: {entity.Id}");
        
        return dbRes;
    }

    public async Task DeleteAsync(TKey? id)
    {
        log.LogInformation($"removing item with id: {id} from cache");
        if (id is null)
        {
            log.LogWarning("they key was null, nothing to cache");
            return;
        }

        await cache.DeleteAsync(id?.ToString());

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
            await cache.SetAsync(entry.Key, entry.Value, entry.Options.Expiry);
            // if(!res)
            //     log.LogWarning($"(inserting to cache failed for id: {entry.Key}");
        }
        
        return dbRes;
    }

    public async Task<T> UpdateAsync(CacheEntry<T> entry)
    {
        var dbRes = await db.UpdateAsync(entry.Value);
        
        if(dbRes != null)
        {
            await cache.SetAsync(entry.Key, entry.Value, entry.Options.Expiry);
        }
        
        return dbRes;
    }

    public async Task<T> UpsertAsync(CacheEntry<T> entry)
    {
        var dbRes = await db.UpsertAsync(entry.Value);
        
        if(dbRes != null)
        {
            await cache.SetAsync(entry.Key, entry.Value, entry.Options.Expiry);
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