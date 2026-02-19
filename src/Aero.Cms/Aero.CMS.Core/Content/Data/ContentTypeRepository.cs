using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Data;
using Aero.CMS.Core.Shared.Interfaces;
using Raven.Client.Documents;

namespace Aero.CMS.Core.Content.Data;

public class ContentTypeRepository : BaseRepository<ContentTypeDocument>, IContentTypeRepository
{
    public ContentTypeRepository(IDocumentStore store, ISystemClock clock) : base(store, clock)
    {
    }

    public async Task<ContentTypeDocument?> GetByAliasAsync(string alias, CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.Query<ContentTypeDocument>()
            .FirstOrDefaultAsync(x => x.Alias == alias, ct);
    }

    public async Task<List<ContentTypeDocument>> GetAllAsync(CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.Query<ContentTypeDocument>()
            .ToListAsync(ct);
    }
}
