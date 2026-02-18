namespace Aero.Social.Twitter.Client.Configuration;

/// <summary>
/// Configuration options for the Twitter API client.
/// </summary>
public class TwitterClientOptions
{
    /// <summary>
    /// Gets or sets the consumer key for OAuth 1.0a authentication.
    /// </summary>
    public string? ConsumerKey { get; set; }

    /// <summary>
    /// Gets or sets the consumer secret for OAuth 1.0a authentication.
    /// </summary>
    public string? ConsumerSecret { get; set; }

    /// <summary>
    /// Gets or sets the access token for OAuth 1.0a authentication.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the access token secret for OAuth 1.0a authentication.
    /// </summary>
    public string? AccessTokenSecret { get; set; }

    /// <summary>
    /// Gets or sets the bearer token for OAuth 2.0 authentication.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed requests.
    /// Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}