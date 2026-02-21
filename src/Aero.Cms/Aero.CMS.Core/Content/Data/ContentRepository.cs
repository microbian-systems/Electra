using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Data;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;
using Aero.CMS.Core.Shared.Services;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace Aero.CMS.Core.Content.Data;

public class ContentRepository : BaseRepository<ContentDocument>, IContentRepository
{
    private readonly SaveHookPipeline<ContentDocument> _hookPipeline;

    public ContentRepository(
        IDocumentStore store, 
        ISystemClock clock,
        ILogger<ContentRepository> log,
        SaveHookPipeline<ContentDocument> hookPipeline) : base(store, clock, log)
    {
        _hookPipeline = hookPipeline;
    }

    public override async Task<HandlerResult> SaveAsync(ContentDocument entity, CancellationToken ct = default)
    {
        try
        {
            await _hookPipeline.RunBeforeAsync(entity);
            
            var result = await base.SaveAsync(entity, ct);
            
            if (result.Success)
            {
                await _hookPipeline.RunAfterAsync(entity);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            return HandlerResult.Fail(ex.Message);
        }
    }

    public async Task<ContentDocument?> GetBySlugAsync(string slug, bool waitForNonStaleResults = false, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        var query = session.Query<ContentDocument>();
        
        if (waitForNonStaleResults)
        {
            query = query.Customize(x => x.WaitForNonStaleResults());
        }

        return await query.FirstOrDefaultAsync(x => x.Slug == slug, ct);
    }

    public async Task<List<ContentDocument>> GetChildrenAsync(Guid? parentId, PublishingStatus? statusFilter = null, bool waitForNonStaleResults = false, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        var query = session.Query<ContentDocument>();

        if (waitForNonStaleResults)
        {
            query = query.Customize(x => x.WaitForNonStaleResults());
        }

        query = query.Where(x => x.ParentId == parentId);

        if (statusFilter.HasValue)
        {
            query = query.Where(x => x.Status == statusFilter.Value);
        }

        return await query.OrderBy(x => x.SortOrder).ToListAsync(ct);
    }

    public async Task<List<ContentDocument>> GetByContentTypeAsync(string contentTypeAlias, bool waitForNonStaleResults = false, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        var query = session.Query<ContentDocument>();

        if (waitForNonStaleResults)
        {
            query = query.Customize(x => x.WaitForNonStaleResults());
        }

        return await query.Where(x => x.ContentTypeAlias == contentTypeAlias)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);
    }
}
