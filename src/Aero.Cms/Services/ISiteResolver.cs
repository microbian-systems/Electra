using Aero.Cms.Models;
using Microsoft.AspNetCore.Http;

namespace Aero.Cms.Services
{
    public interface ISiteResolver
    {
        Task<SiteDocument?> ResolveSiteAsync(HttpContext context);
    }
}
