namespace Aero.Social.Twitter.Integration.Builders;

/// <summary>
/// Builder for creating Twitter API error responses.
/// </summary>
public class ErrorResponseBuilder
{
    private int _statusCode = 400;
    private string? _title;
    private string? _detail;
    private string? _errorType;
    private int? _retryAfter;

    /// <summary>
    /// Creates a 401 Unauthorized error.
    /// </summary>
    public static ErrorResponseBuilder Unauthorized()
    {
        return new ErrorResponseBuilder
        {
            _statusCode = 401,
            _title = "Unauthorized",
            _detail = "Authentication credentials are missing or invalid.",
            _errorType = "https://api.twitter.com/2/problems/client-forbidden"
        };
    }

    /// <summary>
    /// Creates a 404 Not Found error.
    /// </summary>
    public static ErrorResponseBuilder NotFound(string resource = "Tweet")
    {
        return new ErrorResponseBuilder
        {
            _statusCode = 404,
            _title = "Not Found",
            _detail = $"The requested {resource} was not found.",
            _errorType = "https://api.twitter.com/2/problems/resource-not-found"
        };
    }

    /// <summary>
    /// Creates a 429 Rate Limit error.
    /// </summary>
    public static ErrorResponseBuilder RateLimited(int retryAfterSeconds = 900)
    {
        return new ErrorResponseBuilder
        {
            _statusCode = 429,
            _title = "Too Many Requests",
            _detail = "Rate limit exceeded. Please wait before retrying.",
            _errorType = "https://api.twitter.com/2/problems/too-many-requests",
            _retryAfter = retryAfterSeconds
        };
    }

    /// <summary>
    /// Creates a 500 Internal Server Error.
    /// </summary>
    public static ErrorResponseBuilder ServerError()
    {
        return new ErrorResponseBuilder
        {
            _statusCode = 500,
            _title = "Internal Server Error",
            _detail = "An unexpected error occurred.",
            _errorType = "https://api.twitter.com/2/problems/server-error"
        };
    }

    /// <summary>
    /// Creates a 503 Service Unavailable error (transient failure).
    /// </summary>
    public static ErrorResponseBuilder ServiceUnavailable()
    {
        return new ErrorResponseBuilder
        {
            _statusCode = 503,
            _title = "Service Unavailable",
            _detail = "Service temporarily unavailable. Please retry.",
            _errorType = "https://api.twitter.com/2/problems/service-unavailable"
        };
    }

    /// <summary>
    /// Sets a custom error detail message.
    /// </summary>
    public ErrorResponseBuilder WithDetail(string detail)
    {
        _detail = detail;
        return this;
    }

    /// <summary>
    /// Builds the error response object.
    /// </summary>
    public object Build()
    {
        var response = new Dictionary<string, object>
        {
            ["status"] = _statusCode,
            ["title"] = _title,
            ["detail"] = _detail,
            ["type"] = _errorType
        };

        if (_retryAfter.HasValue)
        {
            response["retry_after"] = _retryAfter.Value;
        }

        return new { errors = new[] { response } };
    }

    /// <summary>
    /// Builds the error response as JSON.
    /// </summary>
    public string BuildJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(Build());
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => _statusCode;
}