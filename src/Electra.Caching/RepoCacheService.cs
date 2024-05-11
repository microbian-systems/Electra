namespace Electra.Common.Caching;


public class RepoCacheService<TEntity>(
    ICacheRepository<TEntity, Guid> repository,
    ILogger<RepoCacheService<TEntity, Guid>> log)
    : RepoCacheService<TEntity, Guid>(repository, log)
    where TEntity : class;

// todo - remove the Task.Run() from the implementation
public class RepoCacheService<TEntity, TKey>(
    ICacheRepository<TEntity, TKey> repository,
    ILogger<RepoCacheService<TEntity, TKey>> log)
    : CacheServiceBase<TEntity>(log), ICacheService<TEntity, TKey>
    where TEntity : class
    where TKey : IEquatable<TKey>
{
    public override void Delete(string key)
    {
        repository.Delete(key);
    }

    public override Task DeleteAsync(string key)
    {
        return Task.Run(() => Delete(key));
    }

    public override TEntity Get(string key)
    {
        return repository.Get(key);
    }

    public override Task<TEntity> GetAsync(string key)
    {
        return Task.FromResult(Get(key));
    }

    public override IEnumerable<TEntity> GetCollection(string key)
    {
        return repository.GetCollection(key);
    }

    public override Task<IEnumerable<TEntity>> GetCollectionAsync(string key)
    {
        return Task.FromResult(GetCollection(key));
    }

    public override void Set(TEntity entity, string key)
    {
        repository.Set(entity, key);
    }

    public override void Set(TEntity entity, string key, TimeSpan timeSpan)
    {
        repository.Set(entity, key, timeSpan);
    }

    public override Task SetAsync(TEntity entity, string key)
    {
        return Task.Run(() => Set(entity, key));
    }

    public override Task SetAsync(TEntity entity, string key, TimeSpan timeSpan)
    {
        return Task.Run(() => Set(entity, key, timeSpan));
    }

    public override void SetCollection(IEnumerable<TEntity> collection, string key)
    {
        repository.SetCollection(collection, key);
    }

    public override void SetCollection(IEnumerable<TEntity> collection, string key, TimeSpan timeSpan)
    {
        repository.SetCollection(collection, key, timeSpan);
    }

    public override Task SetCollectionAsync(IEnumerable<TEntity> collection, string key)
    {
        return Task.Run(() => SetCollection(collection, key));
    }

    public override Task SetCollectionAsync(IEnumerable<TEntity> collection, string key, TimeSpan timeSpan)
    {
        return Task.Run(() => SetCollection(collection, key, timeSpan));
    }
}