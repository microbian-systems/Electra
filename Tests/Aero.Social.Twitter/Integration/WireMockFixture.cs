using Aero.Social.Twitter.Client.Clients;
using Aero.Social.Twitter.Client.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.Server;
using WireMock.Settings;

namespace Aero.Social.Twitter.Integration;

/// <summary>
/// Shared test fixture for WireMock-based integration tests.
/// </summary>
public class WireMockFixture : IDisposable
{
    private readonly WireMockServer _server;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Gets the URL of the WireMock server.
    /// </summary>
    public string ServerUrl { get; }

    /// <summary>
    /// Gets the WireMock server instance.
    /// </summary>
    public WireMockServer Server => _server;

    /// <summary>
    /// Gets the service provider for the test context.
    /// </summary>
    public IServiceProvider Services => _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WireMockFixture"/> class.
    /// </summary>
    public WireMockFixture()
    {
        // Start WireMock server on a random port
        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Random port
            UseSSL = false
        });

        ServerUrl = _server.Url!;

        // Configure DI with WireMock as base URL
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient<ITwitterClient, TwitterClient>("TwitterClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(ServerUrl);
            });

        // Add default options - tests can override these
        services.Configure<TwitterClientOptions>(options =>
        {
            options.BearerToken = "test_bearer_token";
            options.ConsumerKey = "test_consumer_key";
            options.ConsumerSecret = "test_consumer_secret";
            options.AccessToken = "test_access_token";
            options.AccessTokenSecret = "test_access_token_secret";
        });
    }

    /// <summary>
    /// Creates a new ITwitterClient instance configured to use the WireMock server.
    /// </summary>
    /// <param name="configureOptions">Optional configuration for options.</param>
    /// <returns>A configured ITwitterClient instance.</returns>
    public ITwitterClient CreateClient(Action<TwitterClientOptions>? configureOptions = null)
    {
        var options = new TwitterClientOptions
        {
            BearerToken = "test_bearer_token"
            // OAuth 1.0a credentials omitted by default to force OAuth 2.0 usage
            // (OAuth 1.0a has issues with relative URLs in tests)
        };

        configureOptions?.Invoke(options);

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(ServerUrl)
        };

        return new TwitterClient(httpClient, Options.Create(options));
    }

    /// <summary>
    /// Resets the WireMock server by clearing all mappings and requests.
    /// </summary>
    public void Reset()
    {
        _server.Reset();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _server?.Stop();
            _server?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Collection definition for integration tests that use the WireMockFixture.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<WireMockFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}