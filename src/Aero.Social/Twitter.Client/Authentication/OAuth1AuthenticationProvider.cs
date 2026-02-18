using System.Security.Cryptography;
using System.Text;
using Aero.Social.Twitter.Client.Configuration;

namespace Aero.Social.Twitter.Client.Authentication;

/// <summary>
/// OAuth 1.0a authentication provider for Twitter API.
/// </summary>
public class OAuth1AuthenticationProvider : IAuthenticationProvider
{
    private readonly TwitterClientOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth1AuthenticationProvider"/> class.
    /// </summary>
    /// <param name="options">The client options containing OAuth credentials.</param>
    public OAuth1AuthenticationProvider(TwitterClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ValidateCredentials();
    }

    /// <inheritdoc />
    public Task AuthenticateRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var oauthParams = BuildOAuthParameters();
        var signatureBase = BuildSignatureBaseString(request, oauthParams);
        var signingKey = $"{_options.ConsumerSecret}&{_options.AccessTokenSecret}";
        var signature = ComputeHmacSha1Signature(signatureBase, signingKey);

        oauthParams.Add("oauth_signature", signature);
        var authHeader = BuildAuthorizationHeader(oauthParams);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", authHeader);

        return Task.CompletedTask;
    }

    private Dictionary<string, string> BuildOAuthParameters()
    {
        return new Dictionary<string, string>
        {
            { "oauth_consumer_key", _options.ConsumerKey! },
            { "oauth_nonce", GenerateNonce() },
            { "oauth_signature_method", "HMAC-SHA1" },
            { "oauth_timestamp", GenerateTimestamp() },
            { "oauth_token", _options.AccessToken! },
            { "oauth_version", "1.0" }
        };
    }

    private string BuildSignatureBaseString(HttpRequestMessage request, Dictionary<string, string> oauthParams)
    {
        var method = request.Method.Method.ToUpperInvariant();
        var uri = request.RequestUri!.GetLeftPart(UriPartial.Path);

        // Collect all parameters
        var allParams = new Dictionary<string, string>(oauthParams);

        // Add query string parameters
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
        foreach (string key in query)
        {
            if (key != null && !allParams.ContainsKey(key))
            {
                allParams.Add(key, query[key]!);
            }
        }

        // Create parameter string (sorted alphabetically)
        var sortedParams = allParams.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{PercentEncode(p.Key)}={PercentEncode(p.Value)}");
        var paramString = string.Join("&", sortedParams);

        // Build signature base string
        return $"{method}&{PercentEncode(uri)}&{PercentEncode(paramString)}";
    }

    private static string ComputeHmacSha1Signature(string baseString, string signingKey)
    {
        using var hasher = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
        var hash = hasher.ComputeHash(Encoding.ASCII.GetBytes(baseString));
        return Convert.ToBase64String(hash);
    }

    private static string BuildAuthorizationHeader(Dictionary<string, string> oauthParams)
    {
        var encoded = oauthParams.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{PercentEncode(p.Key)}=\"{PercentEncode(p.Value)}\"")
            .ToList();
        return string.Join(", ", encoded);
    }

    private static string PercentEncode(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return Uri.EscapeDataString(value)
            .Replace("!", "%21")
            .Replace("'", "%27")
            .Replace("(", "%28")
            .Replace(")", "%29")
            .Replace("*", "%2A");
    }

    private static string GenerateNonce()
    {
        return Guid.NewGuid().ToString("N")[..32];
    }

    private static string GenerateTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    private void ValidateCredentials()
    {
        if (string.IsNullOrEmpty(_options.ConsumerKey))
            throw new InvalidOperationException("ConsumerKey is required for OAuth 1.0a authentication");
        if (string.IsNullOrEmpty(_options.ConsumerSecret))
            throw new InvalidOperationException("ConsumerSecret is required for OAuth 1.0a authentication");
        if (string.IsNullOrEmpty(_options.AccessToken))
            throw new InvalidOperationException("AccessToken is required for OAuth 1.0a authentication");
        if (string.IsNullOrEmpty(_options.AccessTokenSecret))
            throw new InvalidOperationException("AccessTokenSecret is required for OAuth 1.0a authentication");
    }
}