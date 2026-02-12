using System;
using System.Threading.Tasks;

namespace Electra.Common.Web.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationHeaderName = "x-correlation-id";

    public async Task Invoke(HttpContext context)
    {
        // Try to get the correlation ID from the header
        var correlationId = context.Request.Headers.TryGetValue(CorrelationHeaderName, out var headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        // Make it available in the request context
        context.Items[CorrelationHeaderName] = correlationId;

        // Optionally add it to the response header (helps with debugging)
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Use a logging scope so all logs include the CorrelationId
        using (logger.BeginScope(new System.Collections.Generic.Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId
               }))
        {
            await next(context);
        }
    }
}
