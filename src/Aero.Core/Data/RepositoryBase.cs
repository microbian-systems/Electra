using System.Linq.Expressions;
using Aero.Core.Entities;

namespace Aero.EfCore.Data;

public abstract class RepositoryBase<T, TKey>(ILogger<RepositoryBase<T, TKey>> log) 
    : IWriteRepository<T, TKey>
    where T : EntityBase<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected ILogger<RepositoryBase<T, TKey>> log = log;

    public abstract IEnumerable<T> GetAll();
    public abstract T FindById(TKey id);
    public abstract IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
    public abstract Task<IEnumerable<T>> GetAllAsync();
    public abstract Task<T> FindByIdAsync(TKey id);
    public abstract Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    public abstract Task<T> AddAsync(T entity);
    public abstract Task AddAsync(IEnumerable<T> entities);
    public abstract Task<long> RemoveAllAsync();
    public abstract Task RemoveAsync(IEnumerable<TKey> ids);
    public abstract Task RemoveAsync(TKey id);
    public abstract Task RemoveAsync(T entity);
    public abstract Task RemoveAsync(IEnumerable<T> entities);
    public abstract Task SaveAsync(IEnumerable<T> entities);
    public abstract Task<T> SaveAsync(T entity);
}