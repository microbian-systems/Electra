using LanguageExt;

namespace Aero.Caching;

public abstract class CacheServiceBase(ILogger<CacheServiceBase> log)
    : ICacheService
{
    public abstract void Delete(string key);
    public abstract void Set<T>(string key, IEnumerable<T> value, TimeSpan? absoluteExpiration = null);
    public abstract Task DeleteAsync(string key);
    public abstract Option<T> Get<T>(string key);
    public abstract Task<Option<T>> GetAsync<T>(string key);
    public abstract Option<T> GetOrSet<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null);
    public abstract Task<Option<T>> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);
    public abstract bool KeyExists(string key);
    public abstract Task<bool> KeyExistsAsync(string key);
    public abstract void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null);
    public abstract Task SetAsync<T>(string key, IEnumerable<T> value, TimeSpan? absoluteExpiration = null);
    public abstract Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null);
    public abstract long Decrement(string key, long value = 1);
    public abstract Task<long> DecrementAsync(string key, long value = 1);
    public abstract Option<T> HashGet<T>(string key, string field);
    public abstract Task<Option<T>> HashGetAsync<T>(string key, string field);
    public abstract Option<Dictionary<string, T>> HashGetAll<T>(string key);
    public abstract Task<Option<Dictionary<string, T>>> HashGetAllAsync<T>(string key);
    public abstract bool HashSet<T>(string key, string field, T value);
    public abstract Task<bool> HashSetAsync<T>(string key, string field, T value);
    public abstract long Increment(string key, long value = 1);
    public abstract Task<long> IncrementAsync(string key, long value = 1);
}