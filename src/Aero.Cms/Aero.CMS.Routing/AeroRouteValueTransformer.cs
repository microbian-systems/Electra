using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Aero.CMS.Routing;

public class AeroRouteValueTransformer(ContentFinderPipeline pipeline) : DynamicRouteValueTransformer
{
    public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;
        
        // Skip system, static, and admin routes
        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            path.Contains(".") || // Likely a static file
            path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            return values;
        }

        // Extract slug: trim leading and trailing slashes
        var slug = path.Trim('/') ?? string.Empty;
        slug = string.IsNullOrEmpty(slug) ? "/" : $"/{slug}";

        var context = new ContentFinderContext
        {
            Slug = slug,
            HttpContext = httpContext,
            // We can extend this later to extract LanguageCode, IsPreview from query/cookies
            IsPreview = httpContext.Request.Query.ContainsKey("preview")
        };

        var content = await pipeline.ExecuteAsync(context);

        if (content == null)
        {
            return values; // Or return null to continue to next route
        }

        httpContext.Items["AeroContent"] = content;

        return new RouteValueDictionary
        {
            { "controller", "AeroRender" },
            { "action", "Index" },
            { "slug", slug }
        };
    }
}
