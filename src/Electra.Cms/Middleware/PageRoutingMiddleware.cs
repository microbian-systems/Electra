using System.Threading.Tasks;
using Electra.Cms.Services;
using Microsoft.AspNetCore.Http;

namespace Electra.Cms.Middleware
{
    public class PageRoutingMiddleware
    {
        private readonly RequestDelegate _next;

        public PageRoutingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IPageRouter pageRouter, ICmsContext cmsContext)
        {
            if (cmsContext.Site != null)
            {
                var path = context.Request.Path.Value ?? "/";
                var page = await pageRouter.RouteRequestAsync(cmsContext.Site.Id, path);

                if (page != null)
                {
                    cmsContext.Page = page;
                }
            }

            await _next(context);
        }
    }
}
