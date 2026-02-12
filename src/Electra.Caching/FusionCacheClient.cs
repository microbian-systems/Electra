using LanguageExt;
using ZiggyCreatures.Caching.Fusion;

namespace Electra.Caching;

public interface IFusionCacheClient : ICacheService;

public sealed class FusionCacheClient(IFusionCache cache, ILogger<CacheServiceBase> log)
    : CacheServiceBase(log), IFusionCacheClient
{
    private readonly IFusionCache cache = cache;
    public override void Delete(string key)
        => DeleteAsync(key).GetAwaiter().GetResult();

    public override async Task SetAsync<T>(string key, IEnumerable<T> value, TimeSpan? absoluteExpiration = null)
    {
        await cache.SetAsync(key, value);
    }

    public override void Set<T>(string key, IEnumerable<T> value, TimeSpan? absoluteExpiration = null)
        => SetAsync(key, value, absoluteExpiration).GetAwaiter().GetResult();

    public override async Task<bool> KeyExistsAsync(string key)
    {
        var res = await cache.TryGetAsync<object>(key);
        return res.HasValue;
    }

    public override void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        cache.Set(key, value, opts => opts.Duration = absoluteExpiration ?? TimeSpan.FromMinutes(5));
    }

    public override async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        await cache.SetAsync(key, value, opts => opts.Duration = absoluteExpiration ?? TimeSpan.FromMinutes(5));
    }

    public override long Decrement(string key, long value = 1)
    {
        // FusionCache doesn't have atomic increment/decrement, so we simulate it
        // Note: This is not truly atomic - in production you might want Redis for this
        var current = Get<long?>(key).IfNone(0);
        var newValue = current - value;
        Set(key, newValue);
        return newValue ?? 0;
    }

    public override async Task<bool> HashSetAsync<T>(string key, string field, T value)
    {
        // FusionCache doesn't have native hash support, so we simulate it with nested keys
        var hashKey = $"{key}:hash:{field}";
        await cache.SetAsync(hashKey, value);
        return true;
    }

    public override long Increment(string key, long value = 1)
    {
        // FusionCache doesn't have atomic increment/decrement, so we simulate it
        // Note: This is not truly atomic - in production you might want Redis for this
        var current = Get<long?>(key).IfNone(0);
        var newValue = current + value;
        Set(key, newValue);
        return newValue ?? 0;
    }

    public override async Task<long> IncrementAsync(string key, long value = 1)
    {
        // FusionCache doesn't have atomic increment/decrement, so we simulate it
        var current = (await GetAsync<long?>(key)).IfNone(0);
        var newValue = current + value;
        await SetAsync(key, newValue);
        return newValue.Value;
    }

    public override async Task<long> DecrementAsync(string key, long value = 1)
    {
        // FusionCache doesn't have atomic increment/decrement, so we simulate it
        var current = (await GetAsync<long?>(key)).IfNone(0);
        var newValue = current - value;
        await SetAsync(key, newValue);
        return newValue ?? 0;
    }

    public override Option<T> HashGet<T>(string key, string field)
    {
        // FusionCache doesn't have native hash support, so we simulate it with nested keys
        var hashKey = $"{key}:hash:{field}";
        return Get<T>(hashKey);
    }

    public override async Task<Option<Dictionary<string, T>>> HashGetAllAsync<T>(string key)
    {
        // FusionCache doesn't have native hash support
        // This is a limitation - we can't efficiently get all hash fields without maintaining metadata
        log.LogWarning("HashGetAllAsync is not efficiently supported by FusionCache. Consider using Redis for hash operations.");
        return Option<Dictionary<string, T>>.None;
    }

    public override bool HashSet<T>(string key, string field, T value)
    {
        // FusionCache doesn't have native hash support, so we simulate it with nested keys
        var hashKey = $"{key}:hash:{field}";
        Set(hashKey, value);
        return true;
    }

    public override async Task<Option<T>> HashGetAsync<T>(string key, string field)
    {
        // FusionCache doesn't have native hash support, so we simulate it with nested keys
        var hashKey = $"{key}:hash:{field}";
        return await GetAsync<T>(hashKey);
    }

    public override Option<Dictionary<string, T>> HashGetAll<T>(string key)
    {
        // FusionCache doesn't have native hash support
        // This is a limitation - we can't efficiently get all hash fields without maintaining metadata
        log.LogWarning("HashGetAll is not efficiently supported by FusionCache. Consider using Redis for hash operations.");
        return Option<Dictionary<string, T>>.None;
    }

    public override async Task DeleteAsync(string key)
    {
        await cache.RemoveAsync(key);
    }

    public override Option<T> Get<T>(string key)
    {
        var res = cache.TryGet<T>(key);
        return res.HasValue ? Option<T>.Some(res.Value) : Option<T>.None;
    }

    public override async Task<Option<T>> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null)
    {
        var result = await cache.GetOrSetAsync<T>(key, 
            async ct => await factory(), // Wrap the factory to accept CancellationToken
            opts => 
            {
                opts.Duration = absoluteExpiration ?? TimeSpan.FromMinutes(5);
            });

        return result != null ? Option<T>.Some(result) : Option<T>.None;
    }

    public override bool KeyExists(string key)
    {
        var res = cache.TryGet<object>(key);
        return res.HasValue;
    }

    public override async Task<Option<T>> GetAsync<T>(string key)
    {
        var res = await cache.TryGetAsync<T>(key);
        return res.HasValue ? Option<T>.Some(res.Value) : Option<T>.None;
    }

    public override Option<T> GetOrSet<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null)
    {
        var result = cache.GetOrSet<T>(key, ct => factory(), opts => 
        {
            opts.Duration = absoluteExpiration ?? TimeSpan.FromMinutes(5);
        });
        
        return result != null ? Option<T>.Some(result) : Option<T>.None;
    }
}