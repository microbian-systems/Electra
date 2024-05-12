using Electra.Models.Entities;

namespace Electra.Common.Caching;

public interface ICacheRepository<TEntity, TKey> where TEntity : class where TKey : IEquatable<TKey>
{
    IEnumerable<TEntity> GetCollection(string key);
    TEntity Get(string key);
    void Set(TEntity entity, string key);
    void Set(TEntity entity, string key, TimeSpan timeSpan);
    void SetCollection(IEnumerable<TEntity> collection, string key);
    void SetCollection(IEnumerable<TEntity> collection, string key, TimeSpan timeSpan);
    void Delete(string key);
}