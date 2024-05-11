using Foundatio.Caching;

namespace Electra.Common.Caching.Extensions;

public static class FoundatioCacheExtensions
{
    public static int Add<T>(this ICacheClient cache, string key, IDictionary<string, T> value, CacheOptions opts)
        => AddAsync(cache, key, value, opts).GetAwaiter().GetResult();

    public static async Task<int> AddAsync<T>(this ICacheClient cache, string key, IDictionary<string, T> value,
        CacheOptions opts)
    {
        foreach (var item in value)
            await cache.AddAsync(key, value, opts?.Expiry);

        return value.Count;
    }

    public static int AddItems<T>(this ICacheClient cache,
        IEnumerable<(string key, T value, TimeSpan? expiration)> items) =>
        AddItemsAsync(cache, items).GetAwaiter().GetResult();

    /// <summary>
    /// Adds a list of tuples as individual cache entries
    /// </summary>
    /// <param name="cache">the caching object</param>
    /// <param name="items">the items to store</param>
    /// <typeparam name="T">the type of the value to store</typeparam>
    /// <returns></returns>
    public static async Task<int> AddItemsAsync<T>(this ICacheClient cache,
        IEnumerable<(string key, T value, TimeSpan? expiration)> items)
    {
        foreach (var (key, value, expiration) in items)
        {
            var opts = new CacheOptions()
            {
                Expiry = expiration ?? new CacheOptions().Expiry
            };
            var ret = await cache.AddAsync(key, value, opts.Expiry);
        }

        return items.Count();
    }

    public static bool AddDictionary<T>(this ICacheClient cache, string key, IDictionary<string, T> dict,
        CacheOptions opts)
    {
        var ret = cache.Add(key, dict, opts);
        return true;
    }

    public static bool AddDictionary<T>(this ICacheClient cache, CacheEntry<IDictionary<string, T>> entry)
        => AddDictionary<T>(cache, entry.Key, entry.Value, entry.Options);

    public static async Task<bool> AddDictionaryAsync<T>(this ICacheClient cache, string key,
        IDictionary<string, T> dict, CacheOptions opts)
    {
        var ret = await cache.AddAsync(key, dict, opts);
        return true;
    }

    public static async Task<bool> AddDictionaryAsync<T>(this ICacheClient cache,
        CacheEntry<IDictionary<string, T>> entry)
        => await AddDictionaryAsync<T>(cache, entry.Key, entry.Value, entry.Options);


    public static IDictionary<string, T> GetDictionary<T>(this ICacheClient cache, string key)
        => cache.GetDictionaryAsync<T>(key).GetAwaiter().GetResult();

    public static async Task<IDictionary<string, T>> GetDictionaryAsync<T>(this ICacheClient cache, string key)
    {
        var result = await cache.GetAsync<IDictionary<string, T>>(key);
        return result.Value;
    }

    public static int AddCollection<T>(this ICacheClient cache, string key, IEnumerable<T> items,
        CacheOptions opts = null)
    {
        var res = cache.AddAsync(key, items, opts?.Expiry).GetAwaiter().GetResult();
        return items?.Count() ?? 0;
    }

    public static int AddCollection<T>(this ICacheClient cache, CacheEntry<ICollection<T>> entry)
        => AddCollection<T>(cache, entry.Key, entry.Value, entry.Options);

    public static async Task<int> AddCollectionAsync<T>(this ICacheClient cache, string key, IEnumerable<T> items,
        CacheOptions opts)
    {
        var ret = await cache.AddAsync(key, items, opts.Expiry);
        return items.Count();
    }

    public static async Task<int> AddCollectionAsync<T>(this ICacheClient cache, CacheEntry<ICollection<T>> entry)
        => await AddCollectionAsync(cache, entry.Key, entry.Value, entry.Options);

    public static ICollection<T> GetCollection<T>(this ICacheClient cache, string key)
        => cache.GetCollectionAsync<T>(key).GetAwaiter().GetResult();

    public static async Task<ICollection<T>> GetCollectionAsync<T>(this ICacheClient cache, string key)
        => (await cache.GetAsync<ICollection<T>>(key))?.Value;

    public static T GetTuple<T>(this ICacheClient cache, string key)
        => cache.GetTupleAsync<T>(key).GetAwaiter().GetResult();

    public static async Task<T> GetTupleAsync<T>(this ICacheClient cache, string key)
        => (await cache.GetAsync<T>(key)).Value;

    public static bool TryGetItem<T>(this ICacheClient cache, string key, out T value)
        => cache.TryGetItemAsync(key, out value).GetAwaiter().GetResult();

    public static Task<bool> TryGetItemAsync<T>(this ICacheClient cache, string key, out T value)
    {
        var item = cache.GetAsync<T>(key).GetAwaiter().GetResult();

        if (item != null)
        {
            value = item.Value;
            return Task.FromResult(true);
        }

        value = default;
        return Task.FromResult(false);
    }

    public static T GetOrCreate<T>(this ICacheClient cache, string key, Func<T> createItem, CacheOptions opts = null)
        => cache.GetOrCreateAsync(key, createItem, opts).GetAwaiter().GetResult();

    public static async Task<T> GetOrCreateAsync<T>(this ICacheClient cache, string key, Func<T> create,
        CacheOptions opts = null)
    {
        var result = await cache.GetAsync<T>(key);
        if (result.HasValue)
            return result.Value;

        if (opts == null)
            opts = new CacheOptions();

        var item = create();
        var ret = await cache.AddAsync(key, opts?.Expiry);

        return item;
    }

    public static bool ContainsKey(this ICacheClient cache, string key) =>
        cache.ContainsKeyAsync(key).GetAwaiter().GetResult();

    public static async Task<bool> ContainsKeyAsync(this ICacheClient cache, string key)
    {
        var item = await cache.GetAsync<object>(key);
        return item != null;
    }
}