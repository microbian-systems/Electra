using Aero.CMS.Core.Data.Interfaces;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;
using Raven.Client.Documents;

namespace Aero.CMS.Core.Data;

public abstract class BaseRepository<T> : IRepository<T> where T : AuditableDocument
{
    protected readonly IDocumentStore Store;
    protected readonly ISystemClock Clock;

    protected BaseRepository(IDocumentStore store, ISystemClock clock)
    {
        Store = store;
        Clock = clock;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.LoadAsync<T>(id.ToString(), ct);
    }

    public virtual async Task<HandlerResult> SaveAsync(T entity, CancellationToken ct = default)
    {
        try
        {
            using var session = Store.OpenAsyncSession();
            
            var now = Clock.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            
            entity.UpdatedAt = now;
            // Note: CreatedBy/UpdatedBy should be handled by an identity service in future phases

            await session.StoreAsync(entity, entity.Id.ToString(), ct);
            await session.SaveChangesAsync(ct);
            
            return HandlerResult.Ok();
        }
        catch (Exception ex)
        {
            return HandlerResult.Fail(ex.Message);
        }
    }

    public virtual async Task<HandlerResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var session = Store.OpenAsyncSession();
            session.Delete(id.ToString());
            await session.SaveChangesAsync(ct);
            return HandlerResult.Ok();
        }
        catch (Exception ex)
        {
            return HandlerResult.Fail(ex.Message);
        }
    }
}
