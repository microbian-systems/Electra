using Electra.Models.Entities;

namespace Electra.Common.Caching;

public interface ICacheService<TEntity, TKey> where TEntity : class where TKey : IEquatable<TKey>
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


// todo - remove the Task.Run() from the implementation
public class CacheService<TEntity, TKey>(
    ICacheRepository<TEntity, TKey> repository) : ICacheService<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    public virtual void Delete(string key)
    {
        repository.Delete(key);
    }

    public virtual Task DeleteAsync(string key)
    {
        return Task.Run(() => Delete(key));
    }

    public virtual TEntity Get(string key)
    {
        return repository.Get(key);
    }

    public virtual Task<TEntity> GetAsync(string key)
    {
        return Task.FromResult(Get(key));
    }

    public virtual IEnumerable<TEntity> GetCollection(string key)
    {
        return repository.GetCollection(key);
    }

    public virtual Task<IEnumerable<TEntity>> GetCollectionAsync(string key)
    {
        return Task.FromResult(GetCollection(key));
    }

    public virtual void Set(TEntity entity, string key)
    {
        repository.Set(entity, key);
    }

    public virtual void Set(TEntity entity, string key, TimeSpan timeSpan)
    {
        repository.Set(entity, key, timeSpan);
    }

    public virtual Task SetAsync(TEntity entity, string key)
    {
        return Task.Run(() => Set(entity, key));
    }

    public virtual Task SetAsync(TEntity entity, string key, TimeSpan timeSpan)
    {
        return Task.Run(() => Set(entity, key, timeSpan));
    }

    public void SetCollection(IEnumerable<TEntity> collection, string key)
    {
        repository.SetCollection(collection, key);
    }

    public void SetCollection(IEnumerable<TEntity> collection, string key, TimeSpan timeSpan)
    {
        repository.SetCollection(collection, key, timeSpan);
    }

    public Task SetCollectionAsync(IEnumerable<TEntity> collection, string key)
    {
        return Task.Run(() => SetCollection(collection, key));
    }

    public Task SetCollectionAsync(IEnumerable<TEntity> collection, string key, TimeSpan timeSpan)
    {
        return Task.Run(() => SetCollection(collection, key, timeSpan));
    }
}