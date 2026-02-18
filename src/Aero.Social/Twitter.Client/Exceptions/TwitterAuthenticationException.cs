using System.Net;

namespace Aero.Social.Twitter.Client.Exceptions;

/// <summary>
/// Exception thrown when authentication with the Twitter API fails.
/// </summary>
public class TwitterAuthenticationException : TwitterApiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterAuthenticationException"/> class.
    /// </summary>
    public TwitterAuthenticationException()
        : base("Authentication failed. Please check your credentials.", null, HttpStatusCode.Unauthorized)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterAuthenticationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TwitterAuthenticationException(string message)
        : base(message, null, HttpStatusCode.Unauthorized)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterAuthenticationException"/> class with a specified error message
    /// and error context.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorContext">Additional context about the error (e.g., API response).</param>
    public TwitterAuthenticationException(string message, string errorContext)
        : base(message, null, HttpStatusCode.Unauthorized)
    {
        ErrorContext = errorContext;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitterAuthenticationException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public TwitterAuthenticationException(string message, System.Exception innerException)
        : base(message, innerException, HttpStatusCode.Unauthorized)
    {
    }
}