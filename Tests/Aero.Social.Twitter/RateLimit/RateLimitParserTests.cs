using System.Net;
using Aero.Social.Twitter.Client.RateLimit;

namespace Aero.Social.Twitter.RateLimit;

public class RateLimitParserTests
{
    [Fact]
    public void ParseRateLimitHeaders_AllHeadersPresent_ReturnsParsedInfo()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Rate-Limit-Limit", "150");
        response.Headers.Add("X-Rate-Limit-Remaining", "75");
        response.Headers.Add("X-Rate-Limit-Reset", "1234567890");
        response.Headers.Add("Retry-After", "60");

        // Act
        var info = RateLimitParser.ParseRateLimitHeaders(response);

        // Assert
        Assert.NotNull(info);
        Assert.Equal(150, info.Limit);
        Assert.Equal(75, info.Remaining);
        Assert.Equal(1234567890, info.ResetTimestamp);
        Assert.Equal(TimeSpan.FromSeconds(60), info.RetryAfter);
    }

    [Fact]
    public void ParseRateLimitHeaders_NoHeaders_ReturnsNull()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var info = RateLimitParser.ParseRateLimitHeaders(response);

        // Assert
        Assert.Null(info);
    }

    [Fact]
    public void ParseRateLimitHeaders_PartialHeaders_ReturnsPartialInfo()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Rate-Limit-Limit", "100");
        response.Headers.Add("X-Rate-Limit-Remaining", "50");
        // Missing Reset header

        // Act
        var info = RateLimitParser.ParseRateLimitHeaders(response);

        // Assert
        Assert.NotNull(info);
        Assert.Equal(100, info.Limit);
        Assert.Equal(50, info.Remaining);
        Assert.Equal(0, info.ResetTimestamp);
    }

    [Fact]
    public void ParseRateLimitHeaders_InvalidValues_ReturnsPartialInfo()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.Add("X-Rate-Limit-Limit", "invalid");
        response.Headers.Add("X-Rate-Limit-Remaining", "75");
        response.Headers.Add("X-Rate-Limit-Reset", "1234567890");

        // Act
        var info = RateLimitParser.ParseRateLimitHeaders(response);

        // Assert
        Assert.NotNull(info);
        Assert.Equal(0, info.Limit);
        Assert.Equal(75, info.Remaining);
        Assert.Equal(1234567890, info.ResetTimestamp);
    }

    [Fact]
    public void ParseRateLimitHeaders_NullResponse_ReturnsNull()
    {
        // Act
        var info = RateLimitParser.ParseRateLimitHeaders(null);

        // Assert
        Assert.Null(info);
    }

    [Fact]
    public void GetRateLimitDescription_WithValidInfo_ReturnsDescriptiveString()
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 50,
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act
        var description = RateLimitParser.GetRateLimitDescription(info);

        // Assert
        Assert.Contains("50 of 100", description);
        Assert.Contains("50% consumed", description);
    }

    [Fact]
    public void GetRateLimitDescription_WithRateLimit_ReturnsRateLimitedMessage()
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 0,
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act
        var description = RateLimitParser.GetRateLimitDescription(info);

        // Assert
        Assert.Contains("Rate limit exceeded", description);
    }

    [Fact]
    public void GetRateLimitDescription_WithApproachingLimit_ReturnsApproachingMessage()
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 15,  // Less than 20%
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act
        var description = RateLimitParser.GetRateLimitDescription(info);

        // Assert
        Assert.Contains("Approaching rate limit", description);
    }

    [Fact]
    public void GetRateLimitDescription_WithNullInfo_ReturnsNotAvailableMessage()
    {
        // Act
        var description = RateLimitParser.GetRateLimitDescription(null);

        // Assert
        Assert.Equal("Rate limit information not available.", description);
    }

    [Theory]
    [InlineData(100, 5, true)]   // Less than 10% remaining
    [InlineData(100, 0, true)]   // Rate limited
    [InlineData(100, 15, false)] // Above 10% threshold
    [InlineData(100, 50, false)] // Well above threshold
    public void ShouldLogWarning_VariousScenarios_ReturnsExpectedResult(int limit, int remaining, bool expected)
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = limit,
            Remaining = remaining,
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act & Assert
        Assert.Equal(expected, RateLimitParser.ShouldLogWarning(info));
    }

    [Fact]
    public void ShouldLogWarning_WithNullInfo_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(RateLimitParser.ShouldLogWarning(null));
    }

    [Fact]
    public void ParseRateLimitHeaders_OnlyRetryAfter_ReturnsInfoWithRetryAfter()
    {
        // Arrange
        var response = new HttpResponseMessage((HttpStatusCode)429);
        response.Headers.Add("Retry-After", "900");  // 15 minutes

        // Act
        var info = RateLimitParser.ParseRateLimitHeaders(response);

        // Assert
        Assert.NotNull(info);
        Assert.Equal(0, info.Limit);
        Assert.Equal(0, info.Remaining);
        Assert.Equal(TimeSpan.FromSeconds(900), info.RetryAfter);
    }
}