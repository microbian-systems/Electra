using Electra.Cms.Models;
using Microsoft.AspNetCore.Http;

namespace Electra.Cms.Services
{
    public interface ISiteResolver
    {
        Task<SiteDocument?> ResolveSiteAsync(HttpContext context);
    }
}
