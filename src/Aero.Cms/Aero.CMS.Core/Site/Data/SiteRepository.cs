using Aero.CMS.Core.Data;
using Aero.CMS.Core.Data.Interfaces;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Site.Models;
using Raven.Client.Documents;

namespace Aero.CMS.Core.Site.Data;

public interface ISiteRepository : IRepository<SiteDocument>
{
    Task<SiteDocument?> GetDefaultAsync(CancellationToken ct = default);
    Task<List<SiteDocument>> GetAllAsync(CancellationToken ct = default);
}

public class SiteRepository(IDocumentStore store, ISystemClock clock) : BaseRepository<SiteDocument>(store, clock), ISiteRepository
{
    public async Task<SiteDocument?> GetDefaultAsync(CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.Query<SiteDocument>()
            .FirstOrDefaultAsync(x => x.IsDefault, ct);
    }

    public async Task<List<SiteDocument>> GetAllAsync(CancellationToken ct = default)
    {
        using var session = Store.OpenAsyncSession();
        return await session.Query<SiteDocument>()
            .ToListAsync(ct);
    }
}
