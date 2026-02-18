using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Twitter.Client.Logging;

/// <summary>
/// HTTP message handler that logs request and response information.
/// </summary>
public class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpMessageHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingHttpMessageHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        LogRequest(requestId, request);

        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            LogResponse(requestId, response, stopwatch.Elapsed);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{RequestId}] HTTP request failed after {ElapsedMs}ms: {Method} {Url}",
                requestId, stopwatch.ElapsedMilliseconds, request.Method, request.RequestUri);
            throw;
        }
    }

    private void LogRequest(string requestId, HttpRequestMessage request)
    {
        _logger.LogInformation("[{RequestId}] HTTP Request: {Method} {Url}",
            requestId, request.Method, request.RequestUri);

        // Log headers (excluding sensitive ones)
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var header in request.Headers)
            {
                // Skip sensitive headers
                if (IsSensitiveHeader(header.Key))
                {
                    _logger.LogDebug("[{RequestId}] Header: {HeaderName}: [REDACTED]", requestId, header.Key);
                }
                else
                {
                    _logger.LogDebug("[{RequestId}] Header: {HeaderName}: {HeaderValue}",
                        requestId, header.Key, string.Join(", ", header.Value));
                }
            }
        }
    }

    private void LogResponse(string requestId, HttpResponseMessage response, TimeSpan elapsed)
    {
        var level = response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;

        _logger.Log(level, "[{RequestId}] HTTP Response: {StatusCode} {StatusPhrase} in {ElapsedMs}ms",
            requestId,
            (int)response.StatusCode,
            response.ReasonPhrase,
            elapsed.TotalMilliseconds);

        if (!response.IsSuccessStatusCode && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("[{RequestId}] Response headers:", requestId);
            foreach (var header in response.Headers)
            {
                _logger.LogDebug("[{RequestId}]   {HeaderName}: {HeaderValue}",
                    requestId, header.Key, string.Join(", ", header.Value));
            }
        }
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        return headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("X-Api-Key", StringComparison.OrdinalIgnoreCase);
    }
}