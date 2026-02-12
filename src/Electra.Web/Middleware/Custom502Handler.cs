using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Electra.Common.Web.Middleware;

public class Custom502Handler(RequestDelegate next)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task Invoke(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status502BadGateway)
        {
            // Replace the response with a custom error page
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            context.Response.ContentType = "text/html";

            // todo - add custom designed html page to incude
            await context.Response.WriteAsync("<html><body><h1>Whoops! 502 Bad Gateway</h1><p>we are working on resolving this asap...</p></body></html>");
        }
    }
}

public static class Custom502HandlerExtensions
{
    public static IApplicationBuilder UseCustom502Handler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<Custom502Handler>();
    }
}