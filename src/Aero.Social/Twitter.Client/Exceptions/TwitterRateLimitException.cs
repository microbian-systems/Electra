using System.Net;

namespace Aero.Social.Twitter.Client.Exceptions;

/// <summary>
/// Exception thrown when the Twitter API rate limit is exceeded.
/// </summary>
public class TwitterRateLimitException : TwitterApiException
{
    /// <summary>
    /// Gets the time to wait before retrying the request.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterRateLimitException"/> class.
    /// </summary>
    public TwitterRateLimitException()
        : base("Rate limit exceeded. Please wait before retrying.", null, HttpStatusCode.TooManyRequests)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterRateLimitException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TwitterRateLimitException(string message)
        : base(message, null, HttpStatusCode.TooManyRequests)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterRateLimitException"/> class with a specified error message
    /// and retry-after duration.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="retryAfter">The time to wait before retrying the request.</param>
    public TwitterRateLimitException(string message, TimeSpan? retryAfter)
        : base(message, null, HttpStatusCode.TooManyRequests)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterRateLimitException"/> class with a specified error message,
    /// retry-after duration, and error context.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="retryAfter">The time to wait before retrying the request.</param>
    /// <param name="errorContext">Additional context about the error (e.g., API response).</param>
    public TwitterRateLimitException(string message, TimeSpan? retryAfter, string errorContext)
        : base(message, null, HttpStatusCode.TooManyRequests)
    {
        RetryAfter = retryAfter;
        ErrorContext = errorContext;
    }
}