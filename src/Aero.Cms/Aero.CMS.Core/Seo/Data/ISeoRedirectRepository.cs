using Aero.CMS.Core.Seo.Models;

namespace Aero.CMS.Core.Seo.Data;

public interface ISeoRedirectRepository
{
    Task<SeoRedirectDocument?> FindByFromSlugAsync(string? fromSlug);
}