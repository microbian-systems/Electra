using EasyCaching.Core;

namespace Electra.Common.Caching;

public class EasyCachingClient<T>(
    IEasyCachingProvider cache,
    ILogger<CacheServiceBase<T>> log)
    : CacheServiceBase<T>(log) where T : class
{
    public override IEnumerable<T> GetCollection(string key)
        => GetCollectionAsync(key).GetAwaiter().GetResult();

    public override T Get(string key)
        => GetAsync(key).GetAwaiter().GetResult();

    public override void Set(T entity, string key)
        => SetAsync(entity, key).GetAwaiter().GetResult();

    public override void Set(T entity, string key, TimeSpan timeSpan)
        => SetAsync(entity, key, timeSpan).GetAwaiter().GetResult();

    public override void SetCollection(IEnumerable<T> collection, string key)
        => SetCollectionAsync(collection, key).GetAwaiter().GetResult();

    public override void SetCollection(IEnumerable<T> collection, string key, TimeSpan timeSpan)
        => SetCollectionAsync(collection, key, timeSpan).GetAwaiter().GetResult();

    public override void Delete(string key)
        => DeleteAsync(key).GetAwaiter().GetResult();

    public override async Task<IEnumerable<T>> GetCollectionAsync(string key)
    {
        log.LogInformation("getting collection from cache with key: {key}", key);
        var result = await cache.GetAsync<IEnumerable<T>>(key);
        return result.Value;
    }

    public override async Task<T> GetAsync(string key)
    {
        log.LogInformation("getting entity from cache with key: {key}", key);
        var result = await cache.GetAsync<T>(key);
        return result.Value;
    }

    public override async Task SetAsync(T entity, string key)
    {
        log.LogInformation("setting entity in cache with key: {key}", key);
        await cache.SetAsync(key, entity, TimeSpan.FromMinutes(15));
    }

    public override async Task SetAsync(T entity, string key, TimeSpan timeSpan)
    {
        log.LogInformation("setting entity in cache with key: {key}", key);
        await cache.SetAsync(key, entity, timeSpan);
    }

    public override async Task SetCollectionAsync(IEnumerable<T> collection, string key)
    {
        log.LogInformation("setting collection in cache with key: {key}", key);
        await cache.SetAsync(key, collection, TimeSpan.FromMinutes(15));
    }

    public override async Task SetCollectionAsync(IEnumerable<T> collection, string key, TimeSpan timeSpan)
    {
        log.LogInformation("setting collection in cache with key: {key}", key);
        await cache.SetAsync(key, collection, timeSpan);
    }

    public override async Task DeleteAsync(string key)
    {
        log.LogInformation("deleting cache with key: {key}", key);
        await cache.RemoveAsync(key);
    }
}