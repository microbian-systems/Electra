using System.Net;
using Aero.Social.Twitter.Client.Errors;

namespace Aero.Social.Twitter.Errors;

public class TwitterErrorInfoTests
{
    [Theory]
    [InlineData(32, "Could not authenticate you")]
    [InlineData(34, "Sorry, that page does not exist")]
    [InlineData(88, "Rate limit exceeded")]
    [InlineData(144, "No status found with that ID")]
    [InlineData(187, "Status is a duplicate")]
    [InlineData(215, "Bad authentication data")]
    public void GetErrorTitle_KnownErrorCode_ReturnsExpectedTitle(int code, string expectedTitle)
    {
        // Act
        var title = TwitterErrorInfo.GetErrorTitle(code);

        // Assert
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GetErrorTitle_UnknownErrorCode_ReturnsUnknownError()
    {
        // Act
        var title = TwitterErrorInfo.GetErrorTitle(99999);

        // Assert
        Assert.Equal("Unknown Error", title);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(88)]
    [InlineData(144)]
    public void GetSuggestedAction_KnownErrorCode_ReturnsNonEmptyAction(int code)
    {
        // Act
        var action = TwitterErrorInfo.GetSuggestedAction(code);

        // Assert
        Assert.False(string.IsNullOrEmpty(action));
        Assert.DoesNotContain("unexpected error", action);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(88)]
    [InlineData(144)]
    public void GetDocumentationUrl_KnownErrorCode_ReturnsValidUrl(int code)
    {
        // Act
        var url = TwitterErrorInfo.GetDocumentationUrl(code);

        // Assert
        Assert.False(string.IsNullOrEmpty(url));
        Assert.StartsWith("https://", url);
    }

    [Fact]
    public void BuildEnhancedMessage_KnownErrorCode_IncludesAllComponents()
    {
        // Arrange
        int code = 88;
        string apiMessage = "Rate limit exceeded";

        // Act
        var message = TwitterErrorInfo.BuildEnhancedMessage(code, apiMessage);

        // Assert
        Assert.Contains("Twitter API Error 88", message);
        Assert.Contains("Rate limit exceeded", message);
        Assert.Contains("API Message:", message);
        Assert.Contains("Suggested Action:", message);
        Assert.Contains("Documentation:", message);
        Assert.Contains("https://", message);
    }

    [Fact]
    public void BuildEnhancedMessage_NullApiMessage_DoesNotIncludeApiMessage()
    {
        // Arrange
        int code = 88;

        // Act
        var message = TwitterErrorInfo.BuildEnhancedMessage(code, null);

        // Assert
        Assert.Contains("Twitter API Error 88", message);
        Assert.DoesNotContain("API Message:", message);
    }

    [Theory]
    [InlineData(400, true)]   // Bad Request
    [InlineData(404, true)]   // Not Found
    [InlineData(429, true)]   // Too Many Requests
    [InlineData(499, true)]   // Client closed request
    [InlineData(399, false)]  // Just below 4xx
    [InlineData(500, false)]  // Server error
    [InlineData(200, false)]  // OK
    public void IsClientError_VariousStatusCodes_ReturnsExpectedResult(int statusCode, bool expected)
    {
        // Act
        var result = TwitterErrorInfo.IsClientError((HttpStatusCode)statusCode);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(500, true)]   // Internal Server Error
    [InlineData(503, true)]   // Service Unavailable
    [InlineData(504, true)]   // Gateway Timeout
    [InlineData(599, true)]   // Unknown server error
    [InlineData(499, false)]  // Just below 5xx
    [InlineData(400, false)]  // Client error
    [InlineData(200, false)]  // OK
    public void IsServerError_VariousStatusCodes_ReturnsExpectedResult(int statusCode, bool expected)
    {
        // Act
        var result = TwitterErrorInfo.IsServerError((HttpStatusCode)statusCode);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(429, true)]   // Too Many Requests
    [InlineData(428, false)]  // Precondition Required
    [InlineData(430, false)]  // Unknown
    [InlineData(200, false)]  // OK
    public void IsRateLimitError_VariousStatusCodes_ReturnsExpectedResult(int statusCode, bool expected)
    {
        // Act
        var result = TwitterErrorInfo.IsRateLimitError((HttpStatusCode)statusCode);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetErrorInfo_KnownErrorCode_ReturnsAllComponents()
    {
        // Arrange
        int code = 32;

        // Act
        var (title, action, docUrl) = TwitterErrorInfo.GetErrorInfo(code);

        // Assert
        Assert.Equal("Could not authenticate you", title);
        Assert.False(string.IsNullOrEmpty(action));
        Assert.StartsWith("https://", docUrl);
    }

    [Fact]
    public void GetErrorInfo_UnknownErrorCode_ReturnsDefaultComponents()
    {
        // Arrange
        int code = 99999;

        // Act
        var (title, action, docUrl) = TwitterErrorInfo.GetErrorInfo(code);

        // Assert
        Assert.Equal("Unknown Error", title);
        Assert.Contains("unexpected error", action);
        Assert.StartsWith("https://", docUrl);
    }
}