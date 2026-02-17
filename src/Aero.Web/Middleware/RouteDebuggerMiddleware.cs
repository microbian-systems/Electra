using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Aero.Common.Web.Middleware;

public static class RouteDebuggerMiddlewareExtensions
{
    public static IApplicationBuilder UseRouteDebugger(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RouteDebuggerMiddleware>();
    }
}

public class RouteDebuggerMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, IActionDescriptorCollectionProvider provider)
    {
        if (context.Request.Path == "/route-debugger")
        {
            if (provider != null)
            {
                var routes = provider.ActionDescriptors.Items.Select(x => new
                {
                    Action = x.RouteValues["Action"],
                    Controller = x.RouteValues["Controller"],
                    Name = x.AttributeRouteInfo?.Name,
                    Template = x.AttributeRouteInfo?.Template,
                    Constraint = x.ActionConstraints
                }).ToList();

                var routesJson = JsonSerializer.Serialize(routes);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(routesJson, Encoding.UTF8);
            }
            else
            {
                await context.Response.WriteAsync("IActionDescriptorCollectionProvider is null", Encoding.UTF8);
            }
        }
        else
        {
            await next(context);
        }
    }
}


// app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) =>
// {
//     var sb = new StringBuilder();
//     var endpoints = endpointSources.SelectMany(es => es.Endpoints);
//     foreach (var endpoint in endpoints)
//     {
//         if(endpoint is RouteEndpoint routeEndpoint)
//         {
//             _ = routeEndpoint.RoutePattern.RawText;
//             _ = routeEndpoint.RoutePattern.PathSegments;
//             _ = routeEndpoint.RoutePattern.Parameters;
//             _ = routeEndpoint.RoutePattern.InboundPrecedence;
//             _ = routeEndpoint.RoutePattern.OutboundPrecedence;
//         }
//
//         var routeNameMetadata = endpoint.Metadata.OfType<Microsoft.AspNetCore.Routing.RouteNameMetadata>().FirstOrDefault();
//         _ = routeNameMetadata?.RouteName;
//
//         var httpMethodsMetadata = endpoint.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault();
//         _ = httpMethodsMetadata?.HttpMethods; // [GET, POST, ...]
//
//         // There are many more metadata types available...
//     });