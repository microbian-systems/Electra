using LanguageExt;
using Weasel.Postgresql.Tables;
using ZiggyCreatures.Caching.Fusion;

namespace Electra.Caching;

public interface IFusionCacheClient : ICacheService;

public sealed class FusionCacheClient(IFusionCache cache, ILogger<CacheServiceBase> log)
    : CacheServiceBase(log), IFusionCacheClient
{
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
        throw new NotImplementedException();
    }

    public override async Task<bool> HashSetAsync<T>(string key, string field, T value)
    {
        throw new NotImplementedException();
    }

    public override long Increment(string key, long value = 1)
    {
        throw new NotImplementedException();
    }

    public override async Task<long> IncrementAsync(string key, long value = 1)
    {
        throw new NotImplementedException();
    }

    public override async Task<long> DecrementAsync(string key, long value = 1)
    {
        throw new NotImplementedException();
    }

    public override Option<T> HashGet<T>(string key, string field)
    {
        throw new NotImplementedException();
    }

    public override async Task<Option<Dictionary<string, T>>> HashGetAllAsync<T>(string key)
    {
        throw new NotImplementedException();
    }

    public override bool HashSet<T>(string key, string field, T value)
    {
        throw new NotImplementedException();
    }

    public override async Task<Option<T>> HashGetAsync<T>(string key, string field)
    {
        throw new NotImplementedException();
    }

    public override Option<Dictionary<string, T>> HashGetAll<T>(string key)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
        var res = cache.GetOrSet<T>(key, null);
        return res != null ? Option<T>.Some(res) : Option<T>.None;
    }
}