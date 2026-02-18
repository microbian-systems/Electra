using Aero.Social.Twitter.Client.Configuration;

namespace Aero.Social.Twitter.Client.Authentication;

/// <summary>
/// OAuth 2.0 bearer token authentication provider for Twitter API.
/// </summary>
public class OAuth2AuthenticationProvider : IAuthenticationProvider
{
    private readonly TwitterClientOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2AuthenticationProvider"/> class.
    /// </summary>
    /// <param name="options">The client options containing the bearer token.</param>
    public OAuth2AuthenticationProvider(TwitterClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ValidateCredentials();
    }

    /// <inheritdoc />
    public Task AuthenticateRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.BearerToken);
        return Task.CompletedTask;
    }

    private void ValidateCredentials()
    {
        if (string.IsNullOrEmpty(_options.BearerToken))
            throw new InvalidOperationException("BearerToken is required for OAuth 2.0 authentication");
    }
}