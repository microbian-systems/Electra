using Aero.CMS.Core.Data.Interfaces;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Aero.CMS.Core.Data;

public abstract class BaseRepository<T> : IRepository<T> where T : AuditableDocument
{
    protected readonly IDocumentStore Store;
    protected readonly ISystemClock Clock;
    private readonly IAsyncDocumentSession db;
    private readonly ILogger<BaseRepository<T>> log;

    protected BaseRepository(IAsyncDocumentSession db, IDocumentStore store, ISystemClock clock, ILogger<BaseRepository<T>> log)
    {
        this.db = db;
        this.log = log;
        Store = store;
        Clock = clock;
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.LoadAsync<T>(id.ToString(), ct);
    }

    public virtual async Task<HandlerResult> SaveAsync(T entity, CancellationToken ct = default)
    {
        try
        {
            
            
            var now = Clock.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            
            entity.UpdatedAt = now;
            // Note: CreatedBy/UpdatedBy should be handled by an identity service in future phases

            await db.StoreAsync(entity, ct);
            await db.SaveChangesAsync(ct);
            
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
            
            db.Delete(id.ToString());
            await db.SaveChangesAsync(ct);
            return HandlerResult.Ok();
        }
        catch (Exception ex)
        {
            return HandlerResult.Fail(ex.Message);
        }
    }
}
