using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Aero.CMS.Routing;

public class AeroRouteValueTransformer(ContentFinderPipeline pipeline) : DynamicRouteValueTransformer
{
    private static readonly string[] ReservedPrefixes = 
    [
        "/admin", 
        "/_framework", 
        "/_content", 
        "/_blazor", 
        "/not-found", 
        "/api",
        "/css",
        "/js",
        "/lib",
        "/media"
    ];

    public override async ValueTask<RouteValueDictionary?> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        var path = httpContext.Request.Path.Value ?? "/";

        // 1. FAST PATH: Skip reserved routes and static files immediately
        if (IsReserved(path))
        {
            return null;
        }

        // 2. Normalize slug: Ensure it starts with / and has no trailing slash (unless it's the root)
        var slug = path;
        if (string.IsNullOrEmpty(slug) || slug == "/") 
        {
            slug = "/";
        }
        else 
        {
            slug = "/" + slug.Trim('/');
        }

        // 3. Prepare context
        var context = new ContentFinderContext
        {
            Slug = slug,
            HttpContext = httpContext,
            IsPreview = httpContext.Request.Query.ContainsKey("preview"),
            PreviewToken = httpContext.Request.Query["preview"]
        };

        // 4. Find content
        var content = await pipeline.ExecuteAsync(context);

        if (content == null)
        {
            return null; // Next middleware/route takes over
        }

        // 5. Claim the route
        httpContext.Items["AeroContent"] = content;

        return new RouteValueDictionary
        {
            { "controller", "AeroRender" },
            { "action", "Index" }
        };
    }

    private static bool IsReserved(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/") return false;

        var normalizedPath = path.ToLowerInvariant();
        
        // Skip reserved prefixes
        if (ReservedPrefixes.Any(p => normalizedPath.StartsWith(p.ToLowerInvariant())))
            return true;

        // Skip anything with an extension (static files)
        if (path.Contains('.') && !path.EndsWith("/"))
            return true;

        return false;
    }
}
