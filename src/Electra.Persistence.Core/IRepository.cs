using Electra.Core.Entities;

namespace Electra.Persistence.Repositories;



public interface IWriteRepository<T, TKey>
    where T : IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    Task<T> AddAsync(T entity);
    Task AddAsync(IEnumerable<T> entities);
    Task<long> RemoveAllAsync();
    Task RemoveAsync(IEnumerable<TKey> ids);
    Task RemoveAsync(TKey id);
    Task RemoveAsync(T entity);
    Task RemoveAsync(IEnumerable<T> entities);
    Task SaveAsync(IEnumerable<T> entities);
    Task<T> SaveAsync(T entity);
}

public interface IRepository<T, TKey> : IReadOnlyRepository<T, TKey>, IWriteRepository<T, TKey>
    where T : IEntity<TKey>
    where TKey : IEquatable<TKey>
{
}