using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Electra.Cms.Areas.Blog.Services;

public class HashnodeApiHandler : DelegatingHandler
{
    private readonly HashnodeApiOptions _options;
    private readonly ILogger<HashnodeApiHandler> _logger;
    private readonly IConfiguration config;

    public HashnodeApiHandler(IOptions<HashnodeApiOptions> options, IConfiguration config, ILogger<HashnodeApiHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
        this.config = config;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Set base address if not already set
        if (request.RequestUri?.IsAbsoluteUri == false && InnerHandler != null)
        {
            request.RequestUri = new Uri(new Uri(_options.BaseUrl), request.RequestUri);
        }

        // Add Authorization header with Bearer token if configured
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            request.Headers.Remove("Authorization");
            request.Headers.Add("Authorization", _options.ApiKey);
        }

        // Set Content-Type header for GraphQL requests
        if (request.Content != null && !request.Content.Headers.ContentType?.MediaType?.Contains("application/json") == true)
        {
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        }

        // Add User-Agent header
        //if (!request.Headers.UserAgent.Any())
        //{
        //    request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("microbians.io", "1.0"));
        //}

        _logger.LogDebug("Making Hashnode API request to {RequestUri}", request.RequestUri);

        return await base.SendAsync(request, cancellationToken);
    }
}

public class HashnodeApiOptions
{
    public const string SectionName = "HashnodeApi";

    public string BaseUrl { get; set; } = "https://gql.hashnode.com/";
    public string? ApiKey { get; set; }
}