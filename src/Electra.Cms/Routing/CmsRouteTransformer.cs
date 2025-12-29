using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Electra.Cms.Services;
using Microsoft.Extensions.Logging;

namespace Electra.Cms.Routing
{
    public class CmsRouteTransformer : DynamicRouteValueTransformer
    {
        private readonly ICmsContext _cmsContext;
        private readonly ILogger<CmsRouteTransformer> _logger;

        public CmsRouteTransformer(ICmsContext cmsContext, ILogger<CmsRouteTransformer> logger)
        {
            _cmsContext = cmsContext;
            _logger = logger;
        }

        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            _logger.LogDebug("CMS Route Transformer: Checking for page in context. Path: {Path}", httpContext.Request.Path);

            // If the PageRoutingMiddleware found a page, route to the CmsController
            if (_cmsContext.Page != null)
            {
                _logger.LogInformation("CMS Route Transformer: Matched page {PageId} for path {Path}", _cmsContext.Page.Id, httpContext.Request.Path);
                return new ValueTask<RouteValueDictionary>(new RouteValueDictionary
                {
                    { "area", "Cms" },
                    { "controller", "Cms" },
                    { "action", "Index" }
                });
            }

            _logger.LogDebug("CMS Route Transformer: No matched page for path {Path}", httpContext.Request.Path);

            // Return null to continue searching for other routes (hardcoded controllers, etc.)
            return new ValueTask<RouteValueDictionary>((RouteValueDictionary)null!);
        }
    }
}
