using Electra.Core.Entities;

namespace Electra.Persistence;

public interface IReadonlyRepositorySync<T, TKey> where T
    : IEntity<TKey> where TKey
    : IEquatable<TKey>
{
    public IEnumerable<T> GetAll();
    public T FindById(TKey id);
    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate, uint page = 1, uint rows = 10);
}

public interface IReadonlyRepositoryAsync<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public Task<IEnumerable<T>> GetAllAsync();

    public Task<T> FindByIdAsync(TKey id);

    // read here: https://stackoverflow.com/questions/793571/why-would-you-use-expressionfunct-rather-than-funct
    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, uint page = 1, uint rows = 10);
}

public interface IReadOnlyRepository<T, TKey> : IReadonlyRepositorySync<T, TKey>, IReadonlyRepositoryAsync<T, TKey>
    where T : IEntity<TKey> where TKey : IEquatable<TKey>
{
}

public interface IWriteOnlyRepositorySync<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public T Insert(T entity);
    public T Update(T entity);
    public T Upsert(T entity);
    public void Delete(TKey id);
    public void Delete(T entity);
}

public interface IWriteOnlyRepositoryAsync<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public Task<T> InsertAsync(T entity);
    public Task<T> UpdateAsync(T entity);
    public Task<T> UpsertAsync(T entity);
    public Task DeleteAsync(TKey id);
    public Task DeleteAsync(T entity);
}

public interface IWriteOnlyRepository<T, TKey> : IWriteOnlyRepositorySync<T, TKey>, IWriteOnlyRepositoryAsync<T, TKey>
    where T : IEntity<TKey> where TKey : IEquatable<TKey>
{
}

public interface IGenericRepository<T, TKey> : IReadOnlyRepository<T, TKey>, IWriteOnlyRepository<T, TKey>
    where T : IEntity<TKey>, new() where TKey : IEquatable<TKey>
{
}

/// <summary>
/// The main Generic repository for interface for implementing generic repositories.
/// This is for the main database used by the application the majority of the time. If
/// any specific repository is needed, don't swap the DI registration for this. Create a new
/// DI registration for the specific interface & concrete implementation.
/// </summary>
/// <typeparam name="T">The type of data model to be operated upon <see cref="IEntity{TKey}"/></typeparam>
/// <remarks>Guid is the default type for the primary key due to the Electra nature of using document stores</remarks>
public interface IGenericRepository<T> : IGenericRepository<T, Guid> where T : IEntity<Guid>, new()
{
}

public abstract class GenericRepository<T> : GenericRepository<T, Guid>, IGenericRepository<T>
    where T : IEntity<Guid>, new()
{
    protected GenericRepository(ILogger<GenericRepository<T>> log) : base(log)
    {
    }
}

public abstract class GenericRepository<T, TKey> : IGenericRepository<T, TKey>
    where T : IEntity<TKey>, new() where TKey : IEquatable<TKey>
{
    protected readonly ILogger log;

    protected GenericRepository(ILogger log)
    {
        this.log = log;
    }

    public IEnumerable<T> GetAll() => GetAllAsync().GetAwaiter().GetResult();

    public DbContext Context { get; }
    public abstract Task<long> CountAsync();

    public abstract Task<bool> ExistsAsync(TKey id);

    public abstract Task<IEnumerable<T>> GetAllAsync();
    public abstract Task<T> GetByIdAsync(TKey id);

    public abstract Task<IReadOnlyCollection<T>> GetByIdsAsync(IEnumerable<TKey> ids);

    public virtual T FindById(TKey id) => FindByIdAsync(id).GetAwaiter().GetResult();

    public virtual IEnumerable<T> Find(Expression<Func<T, bool>> predicate,
        uint page = 1, uint rows = 20) =>
        FindAsync(predicate, page, rows).GetAwaiter().GetResult();

    public abstract Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate,
        uint page = 1, uint rows = 10);

    public abstract Task<T> FindByIdAsync(TKey id);

    // todo - add overloaded method with IEnumerable<> parameter to all insert/update/delete method
    public virtual T Insert(T entity) => InsertAsync(entity).GetAwaiter().GetResult();

    public virtual T Update(T entity) => UpdateAsync(entity).GetAwaiter().GetResult();

    public virtual T Upsert(T entity) => UpsertAsync(entity).GetAwaiter().GetResult();

    public virtual void Delete(TKey id) => DeleteAsync(id).GetAwaiter().GetResult();

    public virtual void Delete(T entity) => DeleteAsync(entity).GetAwaiter().GetResult();

    public abstract Task<T> InsertAsync(T entity);

    public abstract Task<T> UpdateAsync(T entity);

    public abstract Task<T> UpsertAsync(T entity);

    public abstract Task DeleteAsync(TKey id);

    public abstract Task DeleteAsync(T entity);
}