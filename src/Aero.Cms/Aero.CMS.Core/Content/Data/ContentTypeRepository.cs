using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Data;
using Aero.CMS.Core.Shared.Interfaces;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Aero.CMS.Core.Content.Data;

public class ContentTypeRepository : BaseRepository<ContentTypeDocument>, IContentTypeRepository
{
    public ContentTypeRepository(IDocumentStore store, ISystemClock clock, ILogger<ContentTypeRepository> log) : base(store, clock, log)
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
