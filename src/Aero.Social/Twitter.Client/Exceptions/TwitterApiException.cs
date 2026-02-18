using System.Net;

namespace Aero.Social.Twitter.Client.Exceptions;

/// <summary>
/// Base exception for all Twitter API errors.
/// </summary>
public class TwitterApiException : Exception
{
    /// <summary>
    /// Gets the HTTP status code associated with the error.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterApiException"/> class.
    /// </summary>
    public TwitterApiException()
        : base()
    {
        StatusCode = HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterApiException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TwitterApiException(string message)
        : base(message)
    {
        StatusCode = HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterApiException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TwitterApiException(string message, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterApiException"/> class with a specified error message,
    /// a reference to the inner exception, and the HTTP status code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="statusCode">The HTTP status code associated with the error.</param>
    public TwitterApiException(string message, Exception? innerException, HttpStatusCode statusCode)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets additional context about the error (e.g., the raw API response).
    /// </summary>
    public string? ErrorContext { get; init; }
}