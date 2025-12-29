using System.Threading.Tasks;
using Electra.Cms.Options;
using Electra.Cms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Electra.Cms.Middleware
{
    public class CmsOutputCachingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CmsOptions _options;

        public CmsOutputCachingMiddleware(RequestDelegate next, IOptions<CmsOptions> options)
        {
            _next = next;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context, ICmsContext cmsContext)
        {
            if (!_options.EnableOutputCaching || cmsContext.Page == null || cmsContext.Site == null)
            {
                await _next(context);
                return;
            }

            // Generate ETag
            var etag = ETagGenerator.GenerateETag(cmsContext.Site, cmsContext.Page);

            // Check If-None-Match
            if (context.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var incomingEtag) && incomingEtag == etag)
            {
                context.Response.StatusCode = StatusCodes.Status304NotModified;
                return;
            }

            // Add ETag to response
            if (_options.IncludeETag)
            {
                context.Response.Headers[HeaderNames.ETag] = etag;
            }

            // Set Cache-Control headers
            var cacheControl = new CacheControlHeaderValue
            {
                Public = true,
                MaxAge = System.TimeSpan.FromSeconds(_options.DefaultCacheDurationSeconds)
            };
            context.Response.Headers[HeaderNames.CacheControl] = cacheControl.ToString();

            await _next(context);
        }
    }
}
