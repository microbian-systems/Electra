using System.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;

namespace Electra.Common.Web.Middleware;

public class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString;
        log.LogInformation("Handling request: {method} {path} {queryString}",
            method, path, queryString);
        log.LogInformation("Request Headers: {headers}", context.Request.Headers);
        var url = context.Request.GetDisplayUrl();
        log.LogInformation("Request Url: {url}", url);
        //log.LogInformation($"Request Body: {await GetRequestBody(context.Request)}");
        log.LogDebug("calling next middleware {next}", next);
        await next(context);
        log.LogDebug("finished calling next middleware {next}", next);

        stopwatch.Stop();

        log.LogInformation($"Finished handling request. Response status: {context.Response.StatusCode}. Time taken: {stopwatch.ElapsedMilliseconds} ms");
        log.LogInformation($"Response Headers: {context.Response.Headers}");
    }

    private async Task<string> GetRequestBody(HttpRequest request)
    {
        request.EnableBuffering();
        var bodyStream = new System.IO.StreamReader(request.Body);
        var bodyText = await bodyStream.ReadToEndAsync();
        request.Body.Position = 0;
        return bodyText;
    }
}

public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        return app;
    }
}