using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ILogger = Serilog.ILogger;

namespace Electra.Common.Web.Errors;

/// <summary>
/// Middleware to handle 404 page not found errors
/// </summary>
/// https://joonasw.net/view/custom-error-pages
public class Handle404Middleware
{
    readonly ILogger log;
    readonly RequestDelegate next;

    public Handle404Middleware(RequestDelegate next, ILogger logger)
    {
        log = logger;
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if(context.Response.StatusCode == 404 && !context.Response.HasStarted)
        {
            //Re-execute the request so the user gets the error page
            var originalPath = context.Request.Path.Value;
            context.Items["originalPath"] = originalPath;
            context.Request.Path = "/error/404";
            await next(context);
        } // todo - 404 hndling - do we need another await next(context); here?
    }
}

public static class Handle404MiddlewareRegistration{
    public static IApplicationBuilder Use404Handler(this IApplicationBuilder app) =>
        app.UseMiddleware<Handle404Middleware>();
}