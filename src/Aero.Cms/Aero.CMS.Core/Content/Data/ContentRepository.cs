using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Data;
using Aero.CMS.Core.Shared.Interfaces;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace Aero.CMS.Core.Content.Data;

public class ContentRepository : BaseRepository<ContentDocument>, IContentRepository
{
    public ContentRepository(IDocumentStore store, ISystemClock clock) : base(store, clock)
    {
    }

    public async Task<ContentDocument?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.Query<ContentDocument>()
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);
    }

    public async Task<List<ContentDocument>> GetChildrenAsync(Guid? parentId, PublishingStatus? statusFilter = null, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        var query = session.Query<ContentDocument>()
            .Where(x => x.ParentId == parentId);

        if (statusFilter.HasValue)
        {
            query = query.Where(x => x.Status == statusFilter.Value);
        }

        return await query.OrderBy(x => x.SortOrder).ToListAsync(ct);
    }

    public async Task<List<ContentDocument>> GetByContentTypeAsync(string contentTypeAlias, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.Query<ContentDocument>()
            .Where(x => x.ContentTypeAlias == contentTypeAlias)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);
    }
}
