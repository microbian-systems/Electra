using System.Diagnostics;

namespace Electra.Common.Web.Performance;

public class PerfLoggingMiddleware(RequestDelegate next, ILogger<PerfLoggingMiddleware> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = new Stopwatch();
        sw.Start();
        // Call the next delegate/middleware in the pipeline
        await next(context);
        sw.Stop();
        log.LogInformation(
            $"PerfMon - {context.Request.Protocol} request for {context.Request.Path} took {sw.ElapsedMilliseconds} ms");
    }
}

public static class PerfLoggingMiddlewareRegistration
{
    public static IApplicationBuilder UsePerfLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<PerfLoggingMiddleware>();
}