using System.Linq.Expressions;
using Electra.Core.Entities;
using Electra.Persistence.Core.Functional;
using Microsoft.Extensions.Logging;
using static System.GC;


namespace Electra.Persistence.RavenDB;

public abstract class RavenDbRepositoryBase<TEntity> 
    : GenericRepositoryOption<TEntity>
    where TEntity : IEntity, new()
{
    protected readonly IAsyncDocumentSession session;

    public RavenDbRepositoryBase(IAsyncDocumentSession session, ILogger<GenericRepositoryOption<TEntity>> log) : base(log)
    {
        this.session = session;
    }
    
    public override async Task<long> CountAsync()
    {
        return await session.Query<TEntity>().LongCountAsync();
    }

    public override async Task<bool> ExistsAsync(string id)
    {
        return await session.Query<TEntity>().AnyAsync(x => x.Id == id);
    }

    public override async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        var res = (await session.Query<TEntity>().ToListAsync()) ?? [] ;

        return res;
    }

    public override async Task<Option<TEntity>> FindByIdAsync(string id)
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
            if (existing.IsSome) throw new Exception($"Entity with id: {entity.Id} already exists");
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
            if (existing.IsNone) throw new Exception($"Update failed - Entity with id: {entity.Id} does not exist");
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
            await session.StoreAsync(entity);
            return Some(entity);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to upsert entity with id: {id}", entity.Id);
            return None;
        }
    }

    public override async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var entityOption = await FindByIdAsync(id);
            if (entityOption.IsNone)
            {
                log.LogWarning("Entity with id: {id} not found for deletion", id);
                return false;
            }

            entityOption.IfSome(entity => session.Delete(entity));
            return true;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to delete entity with id: {id}", id);
            return false;
        }
    }

    public override Task<bool> DeleteAsync(TEntity entity)
    {
        try
        {
            session.Delete(entity);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to delete entity with id: {id}", entity.Id);
            return Task.FromResult(false);
        }
        
    }

    public override async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        var results = await session.Query<TEntity>().Where(predicate).ToListAsync();
        return results ?? [];
    }

    public override async Task<Option<TEntity>> GetByIdAsync(string id)
    {
        var res = await FindByIdAsync(id);
        return res;
    }

    public override async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<string> ids)
    {
        return await FindAsync(x => ids.Contains(x.Id));
    }

    public void Dispose()
    {
        session.Dispose();
        SuppressFinalize(this);
    }
}