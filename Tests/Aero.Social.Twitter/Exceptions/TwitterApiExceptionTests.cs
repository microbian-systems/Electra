using System.Net;
using Aero.Social.Twitter.Client.Exceptions;

namespace Aero.Social.Twitter.Exceptions;

public class TwitterApiExceptionTests
{
    [Fact]
    public void TwitterApiException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new TwitterApiException();

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<TwitterApiException>(exception);
    }

    [Fact]
    public void TwitterApiException_MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new TwitterApiException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void TwitterApiException_FullConstructor_ShouldSetAllProperties()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");
        var statusCode = HttpStatusCode.BadRequest;

        // Act
        var exception = new TwitterApiException(message, innerException, statusCode);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(statusCode, exception.StatusCode);
    }

    [Fact]
    public void TwitterApiException_StatusCode_ShouldBeAccessible()
    {
        // Arrange
        var statusCode = HttpStatusCode.NotFound;

        // Act
        var exception = new TwitterApiException("Not found", null, statusCode);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
}

public class TwitterRateLimitExceptionTests
{
    [Fact]
    public void TwitterRateLimitException_ShouldInheritFromTwitterApiException()
    {
        // Act
        var exception = new TwitterRateLimitException("Rate limit exceeded");

        // Assert
        Assert.IsAssignableFrom<TwitterApiException>(exception);
    }

    [Fact]
    public void TwitterRateLimitException_ShouldHave429StatusCode()
    {
        // Act
        var exception = new TwitterRateLimitException("Rate limit exceeded");

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
    }

    [Fact]
    public void TwitterRateLimitException_ShouldStoreRetryAfter()
    {
        // Arrange
        var retryAfter = TimeSpan.FromMinutes(15);

        // Act
        var exception = new TwitterRateLimitException("Rate limit exceeded", retryAfter);

        // Assert
        Assert.Equal(retryAfter, exception.RetryAfter);
    }
}

public class TwitterAuthenticationExceptionTests
{
    [Fact]
    public void TwitterAuthenticationException_ShouldInheritFromTwitterApiException()
    {
        // Act
        var exception = new TwitterAuthenticationException("Authentication failed");

        // Assert
        Assert.IsAssignableFrom<TwitterApiException>(exception);
    }

    [Fact]
    public void TwitterAuthenticationException_ShouldHave401StatusCode()
    {
        // Act
        var exception = new TwitterAuthenticationException("Authentication failed");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }
}