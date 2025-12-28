using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Electra.Common.Extensions;
using Electra.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.Core.EfCore;

public interface IGenericEntityFrameworkRepository<T, TKey> : IGenericRepository<T, TKey>
    where T : IEntity<TKey>, new() where TKey : IEquatable<TKey>
{
    Task<List<T>> GetPaged(int page = 1, int rows = 10);
}
    
public interface IGenericEntityFrameworkRepository<T> : IGenericEntityFrameworkRepository<T, string>, IGenericRepository<T> 
    where T : class, IEntity<string>, new() {}

public class GenericEntityFrameworkRepository<T>(DbContext context, ILogger<GenericEntityFrameworkRepository<T>> log)
    : GenericEntityFrameworkRepository<T, string>(context, log), IGenericRepository<T>
    where T : class, IEntity<string>, new();

public class GenericEntityFrameworkRepository<T, TKey> : GenericRepository<T, TKey>, IGenericEntityFrameworkRepository<T, TKey> where T : class, IEntity<TKey>, new() where TKey : IEquatable<TKey>
{
    protected readonly DbSet<T> db;
    protected readonly DbContext context;

    protected GenericEntityFrameworkRepository(DbContext context, ILogger<GenericEntityFrameworkRepository<T, TKey>> log) : base(log)
    {
        this.db = context.Set<T>();
        this.context = context;
    }
        
    public override async Task<IEnumerable<T>> GetAllAsync() => await Task.FromResult(db.ToList());
        
    public async Task<List<T>> GetPaged(int page = 1, int rows = 10)
    {
        var skip = page <= 1
            ? 0
            : (page - 1) * rows;

        var result = await db.OrderByDescending(x => x.ModifiedOn)
            .Skip(skip)
            .Take(rows)
            .ToListAsync();
            
        return result;
    }

    public override async Task<T> FindByIdAsync(TKey id) => await Task.FromResult(db.Single(x => x.Id.Equals(id)));

    public override async Task<T> InsertAsync(T entity)
    {
        log.LogInformation($"saving entity");
        var results = await db.AddAsync(entity);
        log.LogInformation($"entity saved with id {results.Entity.Id}");
        return results.Entity;
    }

    public override async Task<T> UpdateAsync(T entity)
    {
        log.LogInformation($"updating entity with id {entity.Id}");
        var result = db.Update(entity);
        log.LogInformation($"updated entity");
        return await Task.FromResult(result.Entity);
    }

    public override async Task<T> UpsertAsync(T entity)
    {
        try
        {
            log.LogInformation($"updating/inserting entity with id {entity.Id}");
            
            var exists = db.Any(x => x.Id.Equals(entity.Id));

            if (exists)
            {
                db.Update(entity);
            }
            else
            {
                await db.AddAsync(entity);
            }

            log.LogInformation($"upserted entity with id {entity.Id}");
            
            return entity;
        }
        catch (Exception ex)
        {
            log.LogError(ex,$"error upserting: {entity.ToJson()}");
            throw;
        }
    }

    public override async Task DeleteAsync(TKey id)
    {
        var entity = await Task.FromResult(db.Single(x => x.Id.Equals(id)));
        await DeleteAsync(entity);
    }

    public override async Task DeleteAsync(T entity)
    {
        await Task.CompletedTask;
        var id = entity.Id;
        log.LogInformation($"deleting entity with id {id}");
        var result = context.Remove(entity); 
        log.LogInformation($"deleted entity with id {id}");
    }

    public override async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        log.LogInformation($"quering EF store...");
        var results =  db.Where(predicate); //.ToListAsync();
        return results;
    }
        
    public override Task<long> CountAsync()
    {
        log.LogInformation($"getting count ....");
        var count = (long)db.Count();
        return Task.FromResult(count);
    }

    public override async Task<bool> ExistsAsync(TKey id) => await Task.FromResult(db.Any(x => x.Id.Equals(id)));


    public override async Task<T> GetByIdAsync(TKey id) => await FindByIdAsync(id);

    public override async Task<IReadOnlyCollection<T>> GetByIdsAsync(IEnumerable<TKey> ids)
    {
        var tasks = new List<Task>();
        foreach (var id in ids)
        {
            var test = await db.Where(x => x.Id.Equals(ids.First())).ToListAsync();
                
        }
        throw new NotImplementedException();
    }
}

public abstract class GenericEntityFrameworkRepository<T, TKey, TContext>(
    TContext context,
    ILogger<GenericEntityFrameworkRepository<T, TKey, TContext>> log)
    : GenericRepository<T, TKey>(log),
        IGenericEntityFrameworkRepository<T, TKey>
    where T : class, IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
    where TContext : DbContext
{
    protected readonly DbSet<T> db = context.Set<T>();
    protected readonly TContext context = context;

    public override async Task<IEnumerable<T>> GetAllAsync() => await Task.FromResult(db.ToList());

    public override async Task<T> FindByIdAsync(TKey id) => await Task.FromResult(db.Single(x => x.Id.Equals(id)));

    public override async Task<T> InsertAsync(T entity)
    {
        log.LogInformation($"saving entity");
        var results = await db.AddAsync(entity);
        log.LogInformation($"entity saved with id {results.Entity.Id}");
        return results.Entity;
    }

    public override async Task<T> UpdateAsync(T entity)
    {
        log.LogInformation($"updating entity with id {entity.Id}");
        var result = db.Update(entity);
        log.LogInformation($"updated entity");
        return await Task.FromResult(result.Entity);
    }

    public override async Task<T> UpsertAsync(T entity)
    {
        log.LogInformation($"updating/inserting entity with id {entity.Id}");
        var exists = await Task.FromResult(db.Single(x => x.Id.Equals(entity.Id)));
        var result = (exists == null) ? await db.AddAsync(entity) : db.Update(entity);
        log.LogInformation($"updated entity with id {entity.Id}");
        return result.Entity;
    }

    public override async Task DeleteAsync(TKey id)
    {
        var entity = await Task.FromResult(db.Single(x => x.Id.Equals(id)));
        await DeleteAsync(entity);
    }

    public override async Task DeleteAsync(T entity)
    {
        await Task.Delay(0);

        var id = entity.Id;
        log.LogInformation($"deleting entity with id {id}");
        var result = context.Remove(entity); 
        log.LogInformation($"deleted entity with id {id}");
    }

    public override async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        log.LogInformation($"quering EF store...");
        var results = await  db.Where(predicate).ToListAsync();
        return results;
    }
        
    public override async Task<long> CountAsync()
    {
        log.LogInformation($"getting count ....");
        var count = (long)db.Count();
        return await Task.FromResult(count);
    }

    public override async Task<bool> ExistsAsync(TKey id) => await db.AsQueryable().AnyAsync(x => x.Id.Equals(id));


    public override async Task<T> GetByIdAsync(TKey id) => await FindByIdAsync(id);

    public override async Task<IReadOnlyCollection<T>> GetByIdsAsync(IEnumerable<TKey> ids)
    {
        await Task.Delay(0);
        var tasks = new List<Task>();
        foreach (var id in ids)
        {
        }
        throw new NotImplementedException();
    }
    public async Task<List<T>> GetPaged(int page = 1, int rows = 10)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
