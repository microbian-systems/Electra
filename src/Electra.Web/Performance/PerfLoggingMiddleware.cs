using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Electra.Common.Web.Performance
{
    public class PerfLoggingMiddleware : IMiddleware
    {
        readonly ILogger<PerfLoggingMiddleware> log;
        //readonly RequestDelegate _next;

        public PerfLoggingMiddleware(ILogger<PerfLoggingMiddleware> log)
        {
            this.log = log;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var sw = new Stopwatch();
            sw.Start();
            // Call the next delegate/middleware in the pipeline
            await next(context);
            sw.Stop();
            log.LogInformation($"PerfMon - {context.Request.Protocol} request for {context.Request.Path} took {sw.ElapsedMilliseconds} ms");
        }
    }

    public static class PerfLoggingMiddlewareRegistration{
        public static IApplicationBuilder UsePerfLogging(this IApplicationBuilder app) =>
            app.UseMiddleware<PerfLoggingMiddleware>();
    }
}