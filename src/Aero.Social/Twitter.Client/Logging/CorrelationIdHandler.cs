using Aero.Social.Twitter.Client.Correlation;

namespace Aero.Social.Twitter.Client.Logging;

/// <summary>
/// HTTP message handler that adds correlation IDs to outgoing requests.
/// </summary>
public class CorrelationIdHandler : DelegatingHandler
{
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private const string CorrelationIdHeaderName = "X-Correlation-Id";

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdHandler"/> class.
    /// </summary>
    /// <param name="correlationIdProvider">The provider for generating correlation IDs.</param>
    public CorrelationIdHandler(ICorrelationIdProvider correlationIdProvider)
    {
        _correlationIdProvider = correlationIdProvider ?? new GuidCorrelationIdProvider();
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Only add correlation ID if not already present
        if (!request.Headers.Contains(CorrelationIdHeaderName))
        {
            var correlationId = _correlationIdProvider.GenerateCorrelationId();
            request.Headers.Add(CorrelationIdHeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}