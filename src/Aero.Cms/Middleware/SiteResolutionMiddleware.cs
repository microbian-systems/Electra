using Aero.Cms.Services;
using Microsoft.AspNetCore.Http;

namespace Aero.Cms.Middleware
{
    public class SiteResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public SiteResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ISiteResolver siteResolver, ICmsContext cmsContext)
        {
            var site = await siteResolver.ResolveSiteAsync(context);

            if (site != null)
            {
                cmsContext.Site = site;
            }

            await _next(context);
        }
    }
}
