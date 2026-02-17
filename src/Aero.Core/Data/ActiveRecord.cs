using System.Linq.Expressions;
using Aero.Core.Entities;

namespace Aero.EfCore.Data;

public interface IActiveRecord<T, in TKey> 
    where T: IEntity<TKey> 
    where TKey : IComparable, IEquatable<TKey>
{
    Task<T> Get(TKey key);
    Task Insert(T record);
    Task Update(T record);
    Task Delete(TKey id);
    Task Delete(T record);
    Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression);
}

public abstract class ActiveRecord<T, TKey>(ILogger<ActiveRecord<T, TKey>> log) 
    : IActiveRecord<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>, IComparable
{
    protected readonly ILogger<ActiveRecord<T,TKey>> log = log;

    public abstract Task<T> Get(TKey key);
    public abstract Task Insert(T record);
    public abstract Task Update(T record);
    public abstract Task Delete(TKey id);
    public abstract Task Delete(T record);
    public abstract Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression);
}