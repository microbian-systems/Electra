using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;

namespace Electra.Caching;

/// <summary>
/// A generic caching interface for a variety of caching operations.
/// It supports basic GET/SET, as well as Redis-specific features like hashes and atomic counters.
/// </summary>
public interface ICacheService
{
    Task<Option<T>> GetAsync<T>(string key);
    Option<T> Get<T>(string key);
    Task<Option<T>> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? absoluteExpiration = null);
    Option<T> GetOrSet<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null);
    void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null);
    Task SetAsync<T>(string key, IEnumerable<T> value, TimeSpan? absoluteExpiration = null);
    void Set<T>(string key, IEnumerable<T> value, TimeSpan? absoluteExpiration = null);
    Task DeleteAsync(string key);
    void Delete(string key);
    Task<bool> KeyExistsAsync(string key);
    bool KeyExists(string key);
    Task<long> IncrementAsync(string key, long value = 1);
    long Increment(string key, long value = 1);
    Task<long> DecrementAsync(string key, long value = 1);
    long Decrement(string key, long value = 1);
    Task<bool> HashSetAsync<T>(string key, string field, T value);
    bool HashSet<T>(string key, string field, T value);
    Task<Option<T>> HashGetAsync<T>(string key, string field);
    Option<T> HashGet<T>(string key, string field);
    Task<Option<Dictionary<string, T>>> HashGetAllAsync<T>(string key);
    Option<Dictionary<string, T>> HashGetAll<T>(string key);
}