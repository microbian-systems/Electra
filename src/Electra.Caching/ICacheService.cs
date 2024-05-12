namespace Electra.Common.Caching;

public interface ICacheService<T> where T : class
{
    IEnumerable<T> GetCollection(string key);
    T Get(string key);
    void Set(T entity, string key);
    void Set(T entity, string key, TimeSpan timeSpan);
    void SetCollection(IEnumerable<T> collection, string key);
    void SetCollection(IEnumerable<T> collection, string key, TimeSpan timeSpan);
    void Delete(string key);
    Task<IEnumerable<T>> GetCollectionAsync(string key);
    Task<T> GetAsync(string key);
    Task SetAsync(T entity, string key);
    Task SetAsync(T entity, string key, TimeSpan timeSpan);
    Task SetCollectionAsync(IEnumerable<T> collection, string key);
    Task SetCollectionAsync(IEnumerable<T> collection, string key, TimeSpan timeSpan);
    Task DeleteAsync(string key);
}


public interface ICacheService<TEntity, TKey>
    : ICacheService<TEntity>
    where TEntity :
    class where TKey : IEquatable<TKey>
{
    IEnumerable<TEntity> GetCollection(string key);
    TEntity Get(string key);
    void Set(TEntity entity, string key);
    void Set(TEntity entity, string key, TimeSpan timeSpan);
    void SetCollection(IEnumerable<TEntity> collection, string key);
    void SetCollection(IEnumerable<TEntity> collection, string key, TimeSpan timeSpan);
    void Delete(string key);
    Task<IEnumerable<TEntity>> GetCollectionAsync(string key);
    Task<TEntity> GetAsync(string key);
    Task SetAsync(TEntity entity, string key);
    Task SetAsync(TEntity entity, string key, TimeSpan timeSpan);
    Task SetCollectionAsync(IEnumerable<TEntity> collection, string key);
    Task SetCollectionAsync(IEnumerable<TEntity> collection, string key, TimeSpan timeSpan);
    Task DeleteAsync(string key);
}


public abstract class CacheServiceBase<T>(ILogger<CacheServiceBase<T>> log)
    : ICacheService<T> where T : class
{
    public abstract IEnumerable<T> GetCollection(string key);
    public abstract T Get(string key);
    public abstract void Set(T entity, string key);
    public abstract void Set(T entity, string key, TimeSpan timeSpan);
    public abstract void SetCollection(IEnumerable<T> collection, string key);
    public abstract void SetCollection(IEnumerable<T> collection, string key, TimeSpan timeSpan);
    public abstract void Delete(string key);
    public abstract Task<IEnumerable<T>> GetCollectionAsync(string key);
    public abstract Task<T> GetAsync(string key);
    public abstract Task SetAsync(T entity, string key);
    public abstract Task SetAsync(T entity, string key, TimeSpan timeSpan);
    public abstract Task SetCollectionAsync(IEnumerable<T> collection, string key);
    public abstract Task SetCollectionAsync(IEnumerable<T> collection, string key, TimeSpan timeSpan);
    public abstract Task DeleteAsync(string key);
}