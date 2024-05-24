using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Electra.Common.Web.Logging;

public static class RequestLoggingMiddlewareExtensions
{
    public static void UseSerilogRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}


public class RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger) : IMiddleware
{
        private const string MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        private static readonly HashSet<string> HeaderWhitelist = new() { "Content-Type", "Content-Length", "User-Agent" };
        private readonly ILogger<RequestLoggingMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task InvokeAsync(HttpContext context, RequestDelegate _next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var start = Stopwatch.GetTimestamp();
            try
            {
                await _next(context);
                var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

                var statusCode = context.Response?.StatusCode;
                var level = statusCode > 499 ? LogLevel.Error : LogLevel.Information;

                var logMessage = string.Format(MessageTemplate, context.Request.Method, GetPath(context), statusCode, elapsedMs);
                if (level == LogLevel.Error)
                {
                    var ex = new Exception(logMessage);
                    //LogForErrorContext(context).Log(level, logMessage);
                    logger.LogError(ex, logMessage);
                }
                else
                {
                    _logger.Log(level, logMessage);
                }
            }
            catch (Exception ex)
            {
                var elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());
                if (LogException(context, elapsedMs, ex))
                {
                    throw;
                }
            }
        }

        private bool LogException(HttpContext context, double elapsedMs, Exception ex)
        {
            logger.LogError(ex, "error in request logging middleware");
            // LogForErrorContext(context)
            //     .LogError(ex, MessageTemplate, context.Request.Method, GetPath(context), 500, elapsedMs);

            return false;
        }

        // private ILogger LogForErrorContext(HttpContext context)
        // {
        //     var request = context.Request;
        //
        //     var loggedHeaders = request.Headers
        //         .Where(h => HeaderWhitelist.Contains(h.Key))
        //         .ToDictionary(h => h.Key, h => h.Value.ToString());
        //
        //     var loggerFactory = context.RequestServices.GetService<ILoggerFactory>();
        //     var logger = loggerFactory.CreateLogger("RequestLoggingMiddlewareErrorContext");
        //
        //     foreach (var header in loggedHeaders)
        //     {
        //         logger = logger.ForContext(header.Key, header.Value, true);
        //     }
        //
        //     logger = logger
        //         .ForContext("RequestHost", request.Host)
        //         .ForContext("RequestProtocol", request.Protocol);
        //
        //     return logger;
        // }

        private static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }

        private static string GetPath(HttpContext context)
        {
            return context.Features.Get<IHttpRequestFeature>()?.RawTarget ?? context.Request.Path.ToString();
        }
    }