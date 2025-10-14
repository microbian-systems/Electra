using System.Linq.Expressions;
using Electra.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.ActiveRecord;

public static class ActiveRecordExtensions
{
    public static T GetById<T, TKey>(this T entity, TKey id) 
        where T : IEntity<TKey> where TKey : IComparable, IEquatable<TKey>
    {
        
        return entity;
    }
}


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

public class ActiveRecord<T>(DbContext db, ILogger<ActiveRecord<T, Guid>> log) 
    : ActiveRecord<T, Guid>(db, log) 
    where T : IEntity<Guid>
{
    public override async Task<T> Get(Guid key)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public override async Task Insert(T record)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public override async Task Update(T record)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public override async Task Delete(Guid id)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public override async Task Delete(T record)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public override async Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}

public abstract class ActiveRecord<T, TKey>(DbContext db, ILogger<ActiveRecord<T, TKey>> log) 
    : IActiveRecord<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>, IComparable
{
    protected readonly ILogger<ActiveRecord<T,TKey>> log = log;
    protected readonly DbContext db = db;

    public abstract Task<T> Get(TKey key);
    public abstract Task Insert(T record);
    public abstract Task Update(T record);
    public abstract Task Delete(TKey id);
    public abstract Task Delete(T record);
    public abstract Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression);
}