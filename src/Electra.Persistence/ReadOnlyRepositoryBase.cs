﻿using Electra.Core.Entities;

namespace Electra.Persistence;

public abstract class ReadOnlyRepositoryBase<T, Tkey> : IReadOnlyRepository<T, Tkey> where T : IEntity<Tkey> where Tkey : IEquatable<Tkey>
{
    public DbContext Context { get; }
    public abstract Task<long> CountAsync();
    public abstract Task<bool> ExistsAsync(Tkey id);
    public abstract Task<IEnumerable<T>> GetAllAsync();
    public abstract Task<T> FindByIdAsync(Tkey id);
    public abstract Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    public abstract Task<T> GetByIdAsync(Tkey id);
    public abstract Task<IReadOnlyCollection<T>> GetByIdsAsync(IEnumerable<Tkey> ids);
    public abstract IEnumerable<T> GetAll();
    public abstract T FindById(Tkey id);
    public abstract IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
}