namespace Aero.Social;

public class RefreshTokenException : Exception
{
    public string Identifier { get; }
    public string? ResponseBody { get; }
    public object? RequestBody { get; }

    public RefreshTokenException(string identifier, string? responseBody = null, object? requestBody = null, string? message = null)
        : base(message ?? "Token refresh required")
    {
        Identifier = identifier;
        ResponseBody = responseBody;
        RequestBody = requestBody;
    }
}

public class BadBodyException : Exception
{
    public string Identifier { get; }
    public string? ResponseBody { get; }
    public object? RequestBody { get; }

    public BadBodyException(string identifier, string? responseBody = null, object? requestBody = null, string? message = null)
        : base(message ?? "Bad request body")
    {
        Identifier = identifier;
        ResponseBody = responseBody;
        RequestBody = requestBody;
    }
}

public class NotEnoughScopesException : Exception
{
    public NotEnoughScopesException(string message = "Not enough OAuth scopes granted")
        : base(message)
    {
    }
}

public class RateLimitException : Exception
{
    public TimeSpan? RetryAfter { get; }

    public RateLimitException(TimeSpan? retryAfter = null, string? message = null)
        : base(message ?? "Rate limit exceeded")
    {
        RetryAfter = retryAfter;
    }
}
