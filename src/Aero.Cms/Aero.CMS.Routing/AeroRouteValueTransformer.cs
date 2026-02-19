using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace Aero.CMS.Routing;

public class AeroRouteValueTransformer : DynamicRouteValueTransformer
{
    private readonly ContentFinderPipeline _pipeline;

    public AeroRouteValueTransformer(ContentFinderPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
    {
        var path = httpContext.Request.Path.Value;
        
        // Extract slug: trim leading and trailing slashes
        var slug = path?.Trim('/') ?? string.Empty;

        var context = new ContentFinderContext
        {
            Slug = slug,
            HttpContext = httpContext,
            // We can extend this later to extract LanguageCode, IsPreview from query/cookies
            IsPreview = httpContext.Request.Query.ContainsKey("preview")
        };

        var content = await _pipeline.ExecuteAsync(context);

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
