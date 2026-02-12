using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace Electra.Caching.Extensions;

public static class FusionCacheExtensions
{
    public static int Add<T>(this IFusionCache cache, string key, IDictionary<string, T> value, CacheOptions opts)
        => AddAsync(cache, key, value, opts).GetAwaiter().GetResult();

    public static async Task<int> AddAsync<T>(this IFusionCache cache, string key, IDictionary<string, T> value,
        CacheOptions opts)
    {
        foreach (var item in value)
            await cache.AddAsync(key, value, opts);

        return value.Count;
    }

    public static int AddItems<T>(this IFusionCache cache,
        IEnumerable<(string key, T value, TimeSpan? expiration)> items) =>
        AddItemsAsync(cache, items).GetAwaiter().GetResult();

    /// <summary>
    /// Adds a list of tuples as individual cache entries
    /// </summary>
    /// <param name="cache">the caching object</param>
    /// <param name="items">the items to store</param>
    /// <typeparam name="T">the type of the value to store</typeparam>
    /// <returns></returns>
    public static async Task<int> AddItemsAsync<T>(this IFusionCache cache,
        IEnumerable<(string key, T value, TimeSpan? expiration)> items)
    {
        foreach (var (key, value, expiration) in items)
        {
            var opts = new FusionCacheEntryOptions()
            {
                Duration = expiration ?? TimeSpan.FromMinutes(15)
            };
            await cache.SetAsync(key, value, opts);
        }

        return items.Count();
    }

    public static bool AddDictionary<T>(this IFusionCache cache, string key, IDictionary<string, T> dict,
        CacheOptions opts)
    {
        var ret = cache.Add(key, dict, opts);
        return true;
    }

    public static bool AddDictionary<T>(this IFusionCache cache, CacheEntry<IDictionary<string, T>> entry)
        => AddDictionary<T>(cache, entry.Key, entry.Value, entry.Options);

    public static async Task<bool> AddDictionaryAsync<T>(this IFusionCache cache, string key,
        IDictionary<string, T> dict, CacheOptions opts)
    {
        var ret = await cache.AddAsync(key, dict, opts);
        return true;
    }

    public static async Task<bool> AddDictionaryAsync<T>(this IFusionCache cache,
        CacheEntry<IDictionary<string, T>> entry)
        => await AddDictionaryAsync<T>(cache, entry.Key, entry.Value, entry.Options);


    public static IDictionary<string, T> GetDictionary<T>(this IFusionCache cache, string key)
        => cache.GetDictionaryAsync<T>(key).GetAwaiter().GetResult();

    public static async Task<IDictionary<string, T>> GetDictionaryAsync<T>(this IFusionCache cache, string key)
    {
        var result = await cache.GetOrDefaultAsync<IDictionary<string, T>>(key);
        return result;
    }

    public static int AddCollection<T>(this IFusionCache cache, string key, IEnumerable<T> items,
        CacheOptions opts = null)
    {
        cache.SetAsync(key, items, opts?.Expiry ?? TimeSpan.FromMinutes(15)).GetAwaiter().GetResult();
        return items?.Count() ?? 0;
    }

    public static int AddCollection<T>(this IFusionCache cache, CacheEntry<ICollection<T>> entry)
        => AddCollection<T>(cache, entry.Key, entry.Value, entry.Options);

    public static async Task<int> AddCollectionAsync<T>(this IFusionCache cache, string key, IEnumerable<T> items,
        CacheOptions opts)
    {
        await cache.SetAsync(key, items, opts.Expiry);
        return items.Count();
    }

    public static async Task<int> AddCollectionAsync<T>(this IFusionCache cache, CacheEntry<ICollection<T>> entry)
        => await AddCollectionAsync(cache, entry.Key, entry.Value, entry.Options);

    public static ICollection<T> GetCollection<T>(this IFusionCache cache, string key)
        => cache.GetCollectionAsync<T>(key).GetAwaiter().GetResult();

    public static async Task<ICollection<T>> GetCollectionAsync<T>(this IFusionCache cache, string key)
        => (await cache.GetOrDefaultAsync<ICollection<T>>(key));

    public static T GetTuple<T>(this IFusionCache cache, string key)
        => cache.GetTupleAsync<T>(key).GetAwaiter().GetResult();

    public static async Task<T> GetTupleAsync<T>(this IFusionCache cache, string key)
        => (await cache.GetOrDefaultAsync<T>(key));

    public static bool TryGetItem<T>(this IFusionCache cache, string key, out T value)
        => cache.TryGetItemAsync(key, out value).GetAwaiter().GetResult();

    public static Task<bool> TryGetItemAsync<T>(this IFusionCache cache, string key, out T value)
    {
        var item = cache.GetOrDefaultAsync<T>(key).GetAwaiter().GetResult();

        if (item != null)
        {
            value = item;
            return Task.FromResult(true);
        }

        value = default;
        return Task.FromResult(false);
    }

    public static T GetOrCreate<T>(this IFusionCache cache, string key, Func<T> createItem, CacheOptions opts = null)
        => cache.GetOrCreateAsync(key, createItem, opts).GetAwaiter().GetResult();

    public static async Task<T> GetOrCreateAsync<T>(this IFusionCache cache, string key, Func<T> create,
        CacheOptions opts = null)
    {
        var result = await cache.GetOrDefaultAsync<T>(key);
        if (result is not null)
            return result;

        if (opts == null)
            opts = new CacheOptions();

        var item = create();
        await cache.SetAsync(key, item, opts.Expiry);

        return item;
    }

    public static bool ContainsKey(this IFusionCache cache, string key) =>
        cache.ContainsKeyAsync(key).GetAwaiter().GetResult();

    public static async Task<bool> ContainsKeyAsync(this IFusionCache cache, string key)
    {
        var item = await cache.GetOrDefaultAsync<object>(key);
        return item != null;
    }
}