# Twitter API v2 Client for .NET

A strongly-typed, full-featured, idiomatic Twitter API client for .NET that provides seamless integration with the modern .NET ecosystem.

## Installation

```bash
dotnet add package TwitterApiV2.Client
```

## Quick Start

### OAuth 2.0 (Bearer Token)

```csharp
using Microsoft.Extensions.DependencyInjection;
using TwitterApiV2.Client;
using TwitterApiV2.Client.Extensions;

// Configure services
services.AddTwitterClient(options =>
{
    options.BearerToken = "your_bearer_token_here";
});

// Use the client
public class MyService
{
    private readonly ITwitterClient _twitterClient;

    public MyService(ITwitterClient twitterClient)
    {
        _twitterClient = twitterClient;
    }

    public async Task<Tweet> GetTweetAsync(string tweetId)
    {
        return await _twitterClient.GetTweetAsync(tweetId);
    }
}
```

### OAuth 1.0a

```csharp
services.AddTwitterClient(options =>
{
    options.ConsumerKey = "your_consumer_key";
    options.ConsumerSecret = "your_consumer_secret";
    options.AccessToken = "your_access_token";
    options.AccessTokenSecret = "your_access_token_secret";
});
```

## Features

- **Strongly-typed models** - Full type coverage for request parameters and response payloads
- **OAuth 1.0a & 2.0 support** - Complete authentication support
- **Built-in resilience** - Automatic retry, circuit breaker, and timeout handling
- **Dependency Injection ready** - Seamless integration with `Microsoft.Extensions.DependencyInjection`
- **Async-first** - All operations are asynchronous
- **Configurable** - Easy configuration through `IConfiguration`
- **Structured Logging** - Built-in logging support with Microsoft.Extensions.Logging
- **Request/Response Logging** - Optional detailed HTTP logging with sensitive data redaction
- **Correlation IDs** - Automatic correlation ID generation and propagation for distributed tracing
- **Enhanced Error Messages** - Human-readable error messages with actionable guidance
- **Rate Limit Tracking** - Automatic parsing and exposure of rate limit information

## Configuration

### Using appsettings.json

```json
{
  "TwitterClient": {
    "BearerToken": "your_bearer_token"
  }
}
```

```csharp
services.AddTwitterClient(options =>
    Configuration.GetSection("TwitterClient").Bind(options));
```

## Logging

The client supports structured logging through `Microsoft.Extensions.Logging`:

```csharp
// Inject ILogger<TwitterClient> via DI
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// The TwitterClient will automatically use the logger
services.AddTwitterClient(options =>
{
    options.BearerToken = "your_token";
});
```

### Request/Response Logging

Enable detailed HTTP logging (Authorization headers are automatically redacted):

```csharp
// Add the logging handler to your HttpClient pipeline
services.AddHttpClient("TwitterClient")
    .AddHttpMessageHandler(() => new LoggingHttpMessageHandler
    {
        LogRequestHeaders = true,
        LogResponseHeaders = true
    });
```

## Correlation IDs

Track requests across distributed systems with automatic correlation ID generation:

```csharp
// Add correlation ID support
services.AddHttpClient("TwitterClient")
    .AddHttpMessageHandler(() => new CorrelationIdHandler(
        new GuidCorrelationIdProvider()));

// Or provide your own correlation ID provider
services.AddHttpClient("TwitterClient")
    .AddHttpMessageHandler(() => new CorrelationIdHandler(
        new CustomCorrelationIdProvider()));
```

Correlation IDs are automatically added to:
- HTTP request headers (`X-Correlation-Id`)
- Log scopes for request tracing

## Error Handling

The client throws specific exceptions for different error scenarios:

- `TwitterApiException` - Base exception for all API errors
- `TwitterAuthenticationException` - Authentication failures (401)
- `TwitterRateLimitException` - Rate limit exceeded (429)

### Enhanced Error Messages

Error messages include human-readable descriptions, actionable guidance, and documentation links:

```csharp
try
{
    var tweet = await client.GetTweetAsync("invalid_id");
}
catch (TwitterApiException ex)
{
    // Message includes:
    // - Error code and title
    // - API error message
    // - Suggested action
    // - Documentation URL
    Console.WriteLine(ex.Message);
}
```

Example output:
```
Twitter API Error 144: No status found with that ID
API Message: No status found with that ID.

Suggested Action: The tweet ID you specified does not exist or has been deleted.
Documentation: https://developer.twitter.com/en/docs/twitter-api/v1/tweets/post-and-engage/api-reference/get-statuses-show-id
```

## Rate Limit Tracking

Access rate limit information from API responses:

```csharp
// Get rate limit info from response headers
var rateLimitInfo = RateLimitParser.ParseRateLimitHeaders(response);

if (rateLimitInfo != null)
{
    Console.WriteLine($"Remaining: {rateLimitInfo.Remaining}/{rateLimitInfo.Limit}");
    Console.WriteLine($"Resets in: {rateLimitInfo.TimeUntilReset.TotalMinutes:F1} minutes");
    
    if (rateLimitInfo.IsApproachingLimit)
    {
        Console.WriteLine("Warning: Approaching rate limit!");
    }
}
```

## License

MIT
