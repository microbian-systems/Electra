using Electra.Core.Entities;

namespace Electra.Persistence.Repositories;

public interface IReadonlyRepositorySync<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public IEnumerable<T> GetAll();
    public T FindById(TKey id);
    public IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
}

public interface IReadonlyRepositoryAsync<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>
{
    public Task<IEnumerable<T>> GetAllAsync();

    public Task<T> FindByIdAsync(TKey id);

    // read here: https://stackoverflow.com/questions/793571/why-would-you-use-expressionfunct-rather-than-funct
    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
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
public interface IGenericRepository<T> : IGenericRepository<T, long> where T : IEntity<long>, new()
{
}

public abstract class GenericRepository<T>(ILogger<GenericRepository<T>> log)
    : GenericRepository<T, long>(log), IGenericRepository<T>
    where T : IEntity<long>, new();

public abstract class GenericRepository<T, TKey>(ILogger log) : IGenericRepository<T, TKey>
    where T : IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected readonly ILogger log = log;

    public IEnumerable<T> GetAll() => GetAllAsync().GetAwaiter().GetResult();

    public DbContext Context { get; }
    public abstract Task<long> CountAsync();

    public abstract Task<bool> ExistsAsync(TKey id);

    public abstract Task<IEnumerable<T>> GetAllAsync();
    public abstract Task<T> GetByIdAsync(TKey id);

    public abstract Task<IReadOnlyCollection<T>> GetByIdsAsync(IEnumerable<TKey> ids);

    public virtual T FindById(TKey id) => FindByIdAsync(id).GetAwaiter().GetResult();

    public virtual IEnumerable<T> Find(Expression<Func<T, bool>> predicate) =>
        FindAsync(predicate).GetAwaiter().GetResult();

    public abstract Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

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