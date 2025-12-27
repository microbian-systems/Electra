using System.Linq.Expressions;
using Electra.Core.Entities;
using Electra.Persistence.Core.Functional;
using Microsoft.Extensions.Logging;


namespace Electra.Persistence.RavenDB;

public abstract class RavenDbRepository<TEntity>(IAsyncDocumentSession session, ILogger<GenericRepositoryOption<TEntity>> log) 
    : GenericRepositoryOption<TEntity>(log)
    where TEntity : IEntity, new()
{
    public override async Task<long> CountAsync()
    {
        return await session.Query<TEntity>().LongCountAsync();
    }

    public override async Task<bool> ExistsAsync(long id)
    {
        var res = session.Query<TEntity>().Any(x => x.Id == id);
        return await Task.FromResult(res);
    }

    public override async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        var res = (await session.Query<TEntity>().ToListAsync()) ?? [] ;

        return res;
    }

    public override async Task<Option<TEntity>> FindByIdAsync(long id)
    {
        var entity = await session.Query<TEntity>().FirstOrDefaultAsync(x => x.Id == id);
        var res = entity is not null ? Some(entity) : None;
        return res;
    }

    public override async Task<Option<TEntity>> InsertAsync(TEntity entity)
    {
        try
        {
            var existing = await FindByIdAsync(entity.Id);
            await session.StoreAsync(entity);
            return Some(entity);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to upsert entity with id: {id}", entity.Id);
            return None;
        }
    }

    public override async Task<Option<TEntity>> UpdateAsync(TEntity entity)
    {
        try
        {
            var existing = await FindByIdAsync(entity.Id);
            await session.StoreAsync(entity);
            return Some(entity);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to upsert entity with id: {id}", entity.Id);
            return None;
        }
    }        
    

    public override async Task<Option<TEntity>> UpsertAsync(TEntity entity)
    {
        try
        {
            var existing = await FindByIdAsync(entity.Id);
            await session.StoreAsync(entity);
            return existing;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to upsert entity with id: {id}", entity.Id);
            return None;
        }
    }

    public override async Task<bool> DeleteAsync(long id)
    {
        try
        {
            var entity = await FindByIdAsync(id);
            if (entity.IsNone)
            {
                log.LogWarning("Entity with id: {id} not found for deletion", id);
                return false;
            }

            session.Delete(entity);
            return true;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to delete entity with id: {id}", id);
            return false;
        }
    }

    public override async Task<bool> DeleteAsync(TEntity entity)
    {
        try
        {
            session.Delete(entity);
            return true;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to delete entity with id: {id}", entity.Id);
            return false;
        }
        
    }

    public override async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var results = await session.Query<TEntity>().Where(predicate).ToListAsync();
        return results ?? [];
    }

    public override async Task<Option<TEntity>> GetByIdAsync(long id)
    {
        var res = await FindByIdAsync(id);
        return res;
    }

    public override async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<long> ids)
    {
        return await FindAsync(x => ids.Contains(x.Id));
    }

    public void Dispose()
    {
        session.Dispose();
        GC.SuppressFinalize(this);
    }
}