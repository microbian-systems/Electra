using Aero.CMS.Core.Data;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Seo.Models;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Aero.CMS.Core.Seo.Data;

public class SeoRedirectRepository : BaseRepository<SeoRedirectDocument>, ISeoRedirectRepository
{
    public SeoRedirectRepository(IAsyncDocumentSession db, IDocumentStore store, ISystemClock clock, ILogger<SeoRedirectRepository> log) 
        : base(db, store, clock, log)
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