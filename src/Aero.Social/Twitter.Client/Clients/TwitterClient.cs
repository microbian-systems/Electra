using Aero.Social.Twitter.Client.Authentication;
using Aero.Social.Twitter.Client.Configuration;
using Aero.Social.Twitter.Client.Errors;
using Aero.Social.Twitter.Client.Exceptions;
using Aero.Social.Twitter.Client.Models;
using Aero.Social.Twitter.Client.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aero.Social.Twitter.Client.Clients;

/// <summary>
/// Client for interacting with the Twitter API.
/// </summary>
public class TwitterClient : ITwitterClient
{
    private readonly HttpClient _httpClient;
    private readonly TwitterClientOptions _options;
    private readonly IAuthenticationProvider _authProvider;
    private readonly ILogger<TwitterClient>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="options">The client configuration options.</param>
    /// <param name="logger">Optional logger for diagnostic information.</param>
    public TwitterClient(HttpClient httpClient, IOptions<TwitterClientOptions> options, ILogger<TwitterClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _authProvider = CreateAuthenticationProvider();
            
        _logger?.LogDebug("TwitterClient initialized with base address: {BaseAddress}", httpClient.BaseAddress);
    }

    /// <inheritdoc />
    public async Task<Tweet> GetTweetAsync(string tweetId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tweetId))
        {
            _logger?.LogWarning("GetTweetAsync called with null or empty tweetId");
            throw new ArgumentException("Tweet ID cannot be null or empty", nameof(tweetId));
        }

        // Use relative URL to respect HttpClient.BaseAddress (enables testing with WireMock)
        var url = $"/2/tweets/{tweetId}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        _logger?.LogInformation("Fetching tweet {TweetId}", tweetId);
            
        await _authProvider.AuthenticateRequestAsync(request, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send request for tweet {TweetId}", tweetId);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning("Request for tweet {TweetId} returned status code {StatusCode}", tweetId, (int)response.StatusCode);
            await HandleErrorResponseAsync(response);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = TwitterJsonSerializer.Deserialize<TwitterApiResponse<Tweet>>(json);

        if (result?.Data == null)
        {
            _logger?.LogError("Failed to deserialize tweet response for tweet {TweetId}", tweetId);
            throw new TwitterApiException("Failed to deserialize tweet response");
        }

        _logger?.LogInformation("Successfully retrieved tweet {TweetId}", tweetId);
        return result.Data;
    }

    /// <inheritdoc />
    public async Task<User> GetUserByIdAsync(string userId, UserFields? fields = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger?.LogWarning("GetUserByIdAsync called with null or empty userId");
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        var url = $"/2/users/{userId}";
            
        // Add fields parameter if specified
        if (fields.HasValue && fields.Value != UserFields.None)
        {
            var fieldsString = fields.Value.ToApiString();
            if (!string.IsNullOrEmpty(fieldsString))
            {
                url = $"{url}?user.fields={Uri.EscapeDataString(fieldsString)}";
            }
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        _logger?.LogInformation("Fetching user {UserId}", userId);
            
        await _authProvider.AuthenticateRequestAsync(request, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send request for user {UserId}", userId);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning("Request for user {UserId} returned status code {StatusCode}", userId, (int)response.StatusCode);
            await HandleErrorResponseAsync(response);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = TwitterJsonSerializer.Deserialize<UserResponse>(json);

        if (result?.Data == null)
        {
            _logger?.LogError("Failed to deserialize user response for user {UserId}", userId);
            throw new TwitterApiException("Failed to deserialize user response");
        }

        _logger?.LogInformation("Successfully retrieved user {UserId}", userId);
        return result.Data;
    }

    /// <inheritdoc />
    public async Task<User> GetUserByUsernameAsync(string username, UserFields? fields = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(username))
        {
            _logger?.LogWarning("GetUserByUsernameAsync called with null or empty username");
            throw new ArgumentException("Username cannot be null or empty", nameof(username));
        }

        // Remove @ prefix if present
        var sanitizedUsername = username.StartsWith("@") ? username.Substring(1) : username;

        var url = $"/2/users/by/username/{sanitizedUsername}";
            
        // Add fields parameter if specified
        if (fields.HasValue && fields.Value != UserFields.None)
        {
            var fieldsString = fields.Value.ToApiString();
            if (!string.IsNullOrEmpty(fieldsString))
            {
                url = $"{url}?user.fields={Uri.EscapeDataString(fieldsString)}";
            }
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        _logger?.LogInformation("Fetching user by username {Username}", sanitizedUsername);
            
        await _authProvider.AuthenticateRequestAsync(request, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send request for user {Username}", sanitizedUsername);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning("Request for user {Username} returned status code {StatusCode}", sanitizedUsername, (int)response.StatusCode);
            await HandleErrorResponseAsync(response);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = TwitterJsonSerializer.Deserialize<UserResponse>(json);

        if (result?.Data == null)
        {
            _logger?.LogError("Failed to deserialize user response for username {Username}", sanitizedUsername);
            throw new TwitterApiException("Failed to deserialize user response");
        }

        _logger?.LogInformation("Successfully retrieved user {Username}", sanitizedUsername);
        return result.Data;
    }

    /// <inheritdoc />
    public async Task<TweetResponse> SearchTweetsAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger?.LogWarning("SearchTweetsAsync called with null or empty query");
            throw new ArgumentException("Search query cannot be null or empty", nameof(query));
        }

        // Validate options if provided
        options?.Validate();

        // Build query string
        var queryParams = new System.Collections.Generic.List<string>
        {
            $"query={Uri.EscapeDataString(query)}"
        };

        if (options != null)
        {
            if (options.MaxResults.HasValue)
            {
                queryParams.Add($"max_results={options.MaxResults.Value}");
            }

            if (!string.IsNullOrEmpty(options.SinceId))
            {
                queryParams.Add($"since_id={Uri.EscapeDataString(options.SinceId)}");
            }

            if (!string.IsNullOrEmpty(options.UntilId))
            {
                queryParams.Add($"until_id={Uri.EscapeDataString(options.UntilId)}");
            }

            if (options.StartTime.HasValue)
            {
                queryParams.Add($"start_time={Uri.EscapeDataString(options.StartTime.Value.ToString("O"))}");
            }

            if (options.EndTime.HasValue)
            {
                queryParams.Add($"end_time={Uri.EscapeDataString(options.EndTime.Value.ToString("O"))}");
            }

            if (!string.IsNullOrEmpty(options.NextToken))
            {
                queryParams.Add($"next_token={Uri.EscapeDataString(options.NextToken)}");
            }

            if (options.TweetFields.HasValue && options.TweetFields.Value != TweetFields.None)
            {
                var fieldsString = options.TweetFields.Value.ToApiString();
                if (!string.IsNullOrEmpty(fieldsString))
                {
                    queryParams.Add($"tweet.fields={Uri.EscapeDataString(fieldsString)}");
                }
            }

            if (options.Expansions.HasValue && options.Expansions.Value != ExpansionOptions.None)
            {
                var expansionsString = options.Expansions.Value.ToApiString();
                if (!string.IsNullOrEmpty(expansionsString))
                {
                    queryParams.Add($"expansions={Uri.EscapeDataString(expansionsString)}");
                }
            }

            if (options.UserFields.HasValue && options.UserFields.Value != UserFields.None)
            {
                var userFieldsString = options.UserFields.Value.ToApiString();
                if (!string.IsNullOrEmpty(userFieldsString))
                {
                    queryParams.Add($"user.fields={Uri.EscapeDataString(userFieldsString)}");
                }
            }
        }

        var url = $"/2/tweets/search/recent?{string.Join("&", queryParams)}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        _logger?.LogInformation("Searching tweets with query: {Query}", query);

        await _authProvider.AuthenticateRequestAsync(request, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send search request for query: {Query}", query);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning("Search request returned status code {StatusCode}", (int)response.StatusCode);
            await HandleErrorResponseAsync(response);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = TwitterJsonSerializer.Deserialize<TweetResponse>(json);

        if (result == null)
        {
            _logger?.LogError("Failed to deserialize search response");
            throw new TwitterApiException("Failed to deserialize search response");
        }

        var resultCount = result.Data?.Count ?? 0;
        _logger?.LogInformation("Search completed. Found {Count} tweets", resultCount);
        return result;
    }

    /// <inheritdoc />
    public async Task<TweetResponse> GetUserTweetsAsync(string userId, TimelineOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger?.LogWarning("GetUserTweetsAsync called with null or empty userId");
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        // Validate options if provided
        options?.Validate();

        var queryParams = new System.Collections.Generic.List<string>();

        if (options != null)
        {
            if (options.MaxResults.HasValue)
            {
                queryParams.Add($"max_results={options.MaxResults.Value}");
            }

            if (!string.IsNullOrEmpty(options.SinceId))
            {
                queryParams.Add($"since_id={Uri.EscapeDataString(options.SinceId)}");
            }

            if (!string.IsNullOrEmpty(options.UntilId))
            {
                queryParams.Add($"until_id={Uri.EscapeDataString(options.UntilId)}");
            }

            if (options.StartTime.HasValue)
            {
                queryParams.Add($"start_time={Uri.EscapeDataString(options.StartTime.Value.ToString("O"))}");
            }

            if (options.EndTime.HasValue)
            {
                queryParams.Add($"end_time={Uri.EscapeDataString(options.EndTime.Value.ToString("O"))}");
            }

            if (!string.IsNullOrEmpty(options.PaginationToken))
            {
                queryParams.Add($"pagination_token={Uri.EscapeDataString(options.PaginationToken)}");
            }

            if (!string.IsNullOrEmpty(options.Exclude))
            {
                queryParams.Add($"exclude={Uri.EscapeDataString(options.Exclude)}");
            }

            if (options.TweetFields.HasValue && options.TweetFields.Value != TweetFields.None)
            {
                var fieldsString = options.TweetFields.Value.ToApiString();
                if (!string.IsNullOrEmpty(fieldsString))
                {
                    queryParams.Add($"tweet.fields={Uri.EscapeDataString(fieldsString)}");
                }
            }

            if (options.Expansions.HasValue && options.Expansions.Value != ExpansionOptions.None)
            {
                var expansionsString = options.Expansions.Value.ToApiString();
                if (!string.IsNullOrEmpty(expansionsString))
                {
                    queryParams.Add($"expansions={Uri.EscapeDataString(expansionsString)}");
                }
            }

            if (options.UserFields.HasValue && options.UserFields.Value != UserFields.None)
            {
                var userFieldsString = options.UserFields.Value.ToApiString();
                if (!string.IsNullOrEmpty(userFieldsString))
                {
                    queryParams.Add($"user.fields={Uri.EscapeDataString(userFieldsString)}");
                }
            }
        }

        var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
        var url = $"/2/users/{userId}/tweets{queryString}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        _logger?.LogInformation("Fetching tweets for user {UserId}", userId);

        await _authProvider.AuthenticateRequestAsync(request, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send request for user tweets {UserId}", userId);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning("Request for user tweets {UserId} returned status code {StatusCode}", userId, (int)response.StatusCode);
            await HandleErrorResponseAsync(response);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = TwitterJsonSerializer.Deserialize<TweetResponse>(json);

        if (result == null)
        {
            _logger?.LogError("Failed to deserialize user tweets response for user {UserId}", userId);
            throw new TwitterApiException("Failed to deserialize user tweets response");
        }

        var resultCount = result.Data?.Count ?? 0;
        _logger?.LogInformation("Retrieved {Count} tweets for user {UserId}", resultCount, userId);
        return result;
    }

    /// <inheritdoc />
    public async Task<TweetResponse> GetUserMentionsAsync(string userId, TimelineOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger?.LogWarning("GetUserMentionsAsync called with null or empty userId");
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        // Validate options if provided
        options?.Validate();

        var queryParams = new System.Collections.Generic.List<string>();

        if (options != null)
        {
            if (options.MaxResults.HasValue)
            {
                queryParams.Add($"max_results={options.MaxResults.Value}");
            }

            if (!string.IsNullOrEmpty(options.SinceId))
            {
                queryParams.Add($"since_id={Uri.EscapeDataString(options.SinceId)}");
            }

            if (!string.IsNullOrEmpty(options.UntilId))
            {
                queryParams.Add($"until_id={Uri.EscapeDataString(options.UntilId)}");
            }

            if (options.StartTime.HasValue)
            {
                queryParams.Add($"start_time={Uri.EscapeDataString(options.StartTime.Value.ToString("O"))}");
            }

            if (options.EndTime.HasValue)
            {
                queryParams.Add($"end_time={Uri.EscapeDataString(options.EndTime.Value.ToString("O"))}");
            }

            if (!string.IsNullOrEmpty(options.PaginationToken))
            {
                queryParams.Add($"pagination_token={Uri.EscapeDataString(options.PaginationToken)}");
            }

            if (options.TweetFields.HasValue && options.TweetFields.Value != TweetFields.None)
            {
                var fieldsString = options.TweetFields.Value.ToApiString();
                if (!string.IsNullOrEmpty(fieldsString))
                {
                    queryParams.Add($"tweet.fields={Uri.EscapeDataString(fieldsString)}");
                }
            }

            if (options.Expansions.HasValue && options.Expansions.Value != ExpansionOptions.None)
            {
                var expansionsString = options.Expansions.Value.ToApiString();
                if (!string.IsNullOrEmpty(expansionsString))
                {
                    queryParams.Add($"expansions={Uri.EscapeDataString(expansionsString)}");
                }
            }

            if (options.UserFields.HasValue && options.UserFields.Value != UserFields.None)
            {
                var userFieldsString = options.UserFields.Value.ToApiString();
                if (!string.IsNullOrEmpty(userFieldsString))
                {
                    queryParams.Add($"user.fields={Uri.EscapeDataString(userFieldsString)}");
                }
            }
        }

        var queryString = queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
        var url = $"/2/users/{userId}/mentions{queryString}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        _logger?.LogInformation("Fetching mentions for user {UserId}", userId);

        await _authProvider.AuthenticateRequestAsync(request, cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send request for user mentions {UserId}", userId);
            throw;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogWarning("Request for user mentions {UserId} returned status code {StatusCode}", userId, (int)response.StatusCode);
            await HandleErrorResponseAsync(response);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = TwitterJsonSerializer.Deserialize<TweetResponse>(json);

        if (result == null)
        {
            _logger?.LogError("Failed to deserialize user mentions response for user {UserId}", userId);
            throw new TwitterApiException("Failed to deserialize user mentions response");
        }

        var resultCount = result.Data?.Count ?? 0;
        _logger?.LogInformation("Retrieved {Count} mentions for user {UserId}", resultCount, userId);
        return result;
    }

    private IAuthenticationProvider CreateAuthenticationProvider()
    {
        // Prefer OAuth 1.0a if all credentials are present, otherwise use OAuth 2.0
        if (!string.IsNullOrEmpty(_options.ConsumerKey) &&
            !string.IsNullOrEmpty(_options.ConsumerSecret) &&
            !string.IsNullOrEmpty(_options.AccessToken) &&
            !string.IsNullOrEmpty(_options.AccessTokenSecret))
        {
            _logger?.LogDebug("Using OAuth 1.0a authentication provider");
            return new OAuth1AuthenticationProvider(_options);
        }

        if (!string.IsNullOrEmpty(_options.BearerToken))
        {
            _logger?.LogDebug("Using OAuth 2.0 authentication provider");
            return new OAuth2AuthenticationProvider(_options);
        }

        _logger?.LogError("No authentication credentials configured");
        throw new InvalidOperationException("No authentication credentials configured. Please provide either OAuth 1.0a credentials (ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret) or OAuth 2.0 BearerToken.");
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        var statusCode = response.StatusCode;
        var content = await response.Content.ReadAsStringAsync();

        // Parse error response for enhanced error messages
        var errors = ErrorResponseParser.ParseErrorResponse(content);
        var errorMessage = errors.Count > 0 
            ? ErrorResponseParser.BuildComprehensiveErrorMessage(errors)
            : $"API request failed with status code {(int)statusCode}";

        _logger?.LogError("Twitter API error: Status {StatusCode}, Errors: {ErrorCount}", (int)statusCode, errors.Count);

        switch (statusCode)
        {
            case System.Net.HttpStatusCode.Unauthorized:
                throw new TwitterAuthenticationException(errorMessage, content);
            case System.Net.HttpStatusCode.TooManyRequests:
                // Parse Retry-After header if present
                TimeSpan? retryAfter = null;
                if (response.Headers.TryGetValues("Retry-After", out var retryValues))
                {
                    if (int.TryParse(retryValues.FirstOrDefault(), out var seconds))
                    {
                        retryAfter = TimeSpan.FromSeconds(seconds);
                        _logger?.LogWarning("Rate limit exceeded. Retry after {RetryAfterSeconds} seconds", seconds);
                    }
                }
                throw new TwitterRateLimitException(errorMessage, retryAfter, content);
            case System.Net.HttpStatusCode.NotFound:
                throw new TwitterApiException(errorMessage, null, statusCode);
            default:
                throw new TwitterApiException(errorMessage, null, statusCode);
        }
    }

    private class TwitterApiResponse<T>
    {
        public T? Data { get; set; }
    }
}