using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aero.CMS.Routing;

public class AeroRouteValueTransformer(ContentFinderPipeline pipeline, ILogger<AeroRouteValueTransformer> logger) : DynamicRouteValueTransformer
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
        logger.LogInformation("AeroRouteValueTransformer: Claiming route for slug {Slug}. Mapping to AeroRender/Index.", slug);
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
        
        // Skip reserved prefixes
        if (ReservedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Skip anything with an extension (static files)
        if (path.Contains('.') && !path.EndsWith("/"))
            return true;

        return false;
    }
}
