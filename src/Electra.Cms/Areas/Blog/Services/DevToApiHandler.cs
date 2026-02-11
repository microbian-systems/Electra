using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Electra.Cms.Areas.Blog.Services;

public class DevToApiHandler : DelegatingHandler
{
    private readonly DevToApiOptions _options;
    private readonly ILogger<DevToApiHandler> _logger;

    public DevToApiHandler(IOptions<DevToApiOptions> options, ILogger<DevToApiHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Set base address if not already set
        if (request.RequestUri?.IsAbsoluteUri == false && InnerHandler != null)
        {
            request.RequestUri = new Uri(new Uri(_options.BaseUrl), request.RequestUri);
        }

        // Add API key header if configured
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            request.Headers.Remove("api-key");
            request.Headers.Add("api-key", _options.ApiKey);
        }

        // Add User-Agent header
        if (!request.Headers.UserAgent.Any())
        {
            request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("microbians.io", "1.0"));
        }

        _logger.LogDebug("Making DevTo API request to {RequestUri}", request.RequestUri);

        return await base.SendAsync(request, cancellationToken);
    }
}

public class DevToApiOptions
{
    public const string SectionName = "DevToApi";

    public string BaseUrl { get; set; } = "https://dev.to/api/";
    public string? ApiKey { get; set; }
}