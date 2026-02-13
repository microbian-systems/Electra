using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Electra.Core.Entities;

namespace Electra.Persistence.EfCore.ActiveRecord;

public static class ActiveRecordExtensions
{
    public static T GetById<T, TKey>(this T entity, TKey id) 
        where T : IEntity<TKey> where TKey : IComparable, IEquatable<TKey>
    {
        
        return entity;
    }
}


public class ActiveRecordEfCore<T>(DbContext db, ILogger<ActiveRecordEfCore<T, long>> log) 
    : ActiveRecordEfCore<T, long>(db, log) 
    where T : IEntity<long>
{
    public override async Task<T> Get(long key)
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

    public override async Task Delete(long id)
    {
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

public abstract class ActiveRecordEfCore<T, TKey>(DbContext db, ILogger<ActiveRecordEfCore<T, TKey>> log) 
    : IActiveRecord<T, TKey> where T : IEntity<TKey> where TKey : IEquatable<TKey>, IComparable
{
    protected readonly ILogger<ActiveRecordEfCore<T,TKey>> log = log;
    protected readonly DbContext db = db;

    public abstract Task<T> Get(TKey key);
    public abstract Task Insert(T record);
    public abstract Task Update(T record);
    public abstract Task Delete(TKey id);
    public abstract Task Delete(T record);
    public abstract Task<IEnumerable<T>> Find(Expression<Func<T, bool>> expression);
}