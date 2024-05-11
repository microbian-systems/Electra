using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Electra.Common.Web.Logging
{
    namespace Electra.AspNetCore.Middleware.Logging
    {
        public class RequestLoggingMiddleware : IMiddleware
        {
            const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            static readonly ILogger Log = Serilog.Log.ForContext<RequestLoggingMiddleware>();
            static readonly HashSet<string> HeaderWhitelist = new() { "Content-Type", "Content-Length", "User-Agent" };
            readonly RequestDelegate _next;

            public RequestLoggingMiddleware(RequestDelegate next)
            {
                _next = next ?? throw new ArgumentNullException(nameof(next));
            }

            // ReSharper disable once UnusedMember.Global
            //public async Task Invoke(HttpContext httpContext)
            //{
            //}

            static bool LogException(HttpContext httpContext, double elapsedMs, Exception ex)
            {
                LogForErrorContext(httpContext)
                    .Error(ex, MessageTemplate, httpContext.Request.Method, GetPath(httpContext), 500, elapsedMs);

                return false;
            }

            static ILogger LogForErrorContext(HttpContext httpContext)
            {
                var request = httpContext.Request;

                var loggedHeaders = request.Headers
                    .Where(h => HeaderWhitelist.Contains(h.Key))
                    .ToDictionary(h => h.Key, h => h.Value.ToString());

                var result = Log
                    .ForContext("RequestHeaders", loggedHeaders, destructureObjects: true)
                    .ForContext("RequestHost", request.Host)
                    .ForContext("RequestProtocol", request.Protocol);

                return result;
            }

            static double GetElapsedMilliseconds(long start, long stop)
            {
                return (stop - start) * 1000 / (double)Stopwatch.Frequency;
            }

            static string GetPath(HttpContext httpContext)
            {
                return httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget ?? httpContext.Request.Path.ToString();
            }

            public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
            {
                if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

                var start = Stopwatch.GetTimestamp();
                try
                {
                    await _next(httpContext);
                    var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

                    var statusCode = httpContext.Response?.StatusCode;
                    var level = statusCode > 499 ? LogEventLevel.Error : LogEventLevel.Information;

                    var log = level == LogEventLevel.Error ? LogForErrorContext(httpContext) : Log;
                    log.Write(level, MessageTemplate, httpContext.Request.Method, GetPath(httpContext), statusCode, elapsedMs);
                }
                // Never caught, because `LogException()` returns false.
                catch (Exception ex) when (LogException(httpContext, GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()), ex)) { }
            }
        }

        public static class RequestLoggingMiddlewareExtensions
        {
            public static void UseSerilogRequestLogging(this IApplicationBuilder app) =>
                app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}