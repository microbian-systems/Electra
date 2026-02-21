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
        
        // Skip static files
        if (path.Contains(".") || path.EndsWith("favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            return values;
        }

        // Normalize slug to absolute path format: e.g. "/", "/hello-world"
        var slug = "/" + path.Trim('/');

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
