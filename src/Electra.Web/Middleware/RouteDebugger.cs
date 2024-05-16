using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Electra.Common.Web.Middleware;

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

public static class RouteDebuggerMiddlewareExtensions
{
    public static IApplicationBuilder UseRouteDebugger(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RouteDebuggerMiddleware>();
    }
}