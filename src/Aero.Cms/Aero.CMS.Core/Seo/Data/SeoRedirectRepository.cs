using Aero.CMS.Core.Data;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Seo.Models;
using Raven.Client.Documents;

namespace Aero.CMS.Core.Seo.Data;

public class SeoRedirectRepository : BaseRepository<SeoRedirectDocument>, ISeoRedirectRepository
{
    public SeoRedirectRepository(IDocumentStore store, ISystemClock clock) 
        : base(store, clock)
    {
    }

    public async Task<SeoRedirectDocument?> FindByFromSlugAsync(string? fromSlug)
    {
        if (string.IsNullOrWhiteSpace(fromSlug))
        {
            return null;
        }

        using var session = Store.OpenAsyncSession();
        return await session.Query<SeoRedirectDocument>()
            .Where(x => x.FromSlug == fromSlug && x.IsActive)
            .SingleOrDefaultAsync();
    }
}