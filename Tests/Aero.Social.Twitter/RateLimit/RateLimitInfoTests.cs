using Aero.Social.Twitter.Client.RateLimit;

namespace Aero.Social.Twitter.RateLimit;

public class RateLimitInfoTests
{
    [Fact]
    public void IsRateLimited_RemainingIsZero_ReturnsTrue()
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 0,
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act & Assert
        Assert.True(info.IsRateLimited);
    }

    [Fact]
    public void IsRateLimited_RemainingIsGreaterThanZero_ReturnsFalse()
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = 100,
            Remaining = 1,
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act & Assert
        Assert.False(info.IsRateLimited);
    }

    [Theory]
    [InlineData(100, 20, false)]  // 80% consumed, above 20% threshold
    [InlineData(100, 19, true)]   // 81% consumed, below 20% threshold
    [InlineData(100, 10, true)]   // 90% consumed
    [InlineData(100, 0, false)]   // 100% consumed but remaining is 0
    public void IsApproachingLimit_VariousScenarios_ReturnsExpectedResult(int limit, int remaining, bool expected)
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = limit,
            Remaining = remaining,
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act & Assert
        Assert.Equal(expected, info.IsApproachingLimit);
    }

    [Theory]
    [InlineData(100, 50, 50)]   // 50% consumed
    [InlineData(100, 0, 100)]   // 100% consumed
    [InlineData(100, 100, 0)]   // 0% consumed
    [InlineData(0, 0, 0)]       // Edge case: limit is 0
    public void PercentConsumed_VariousScenarios_ReturnsExpectedPercentage(int limit, int remaining, double expected)
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = limit,
            Remaining = remaining,
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act & Assert
        Assert.Equal(expected, info.PercentConsumed);
    }

    [Fact]
    public void ResetTime_ValidTimestamp_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddMinutes(15);
        var info = new RateLimitInfo
        {
            ResetTimestamp = futureTime.ToUnixTimeSeconds()
        };

        // Act
        var resetTime = info.ResetTime;

        // Assert
        Assert.Equal(futureTime.ToUnixTimeSeconds(), resetTime.ToUnixTimeSeconds());
    }

    [Fact]
    public void TimeUntilReset_FutureResetTime_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var info = new RateLimitInfo
        {
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
        };

        // Act
        var timeUntilReset = info.TimeUntilReset;

        // Assert
        Assert.True(timeUntilReset > TimeSpan.Zero);
        Assert.True(timeUntilReset.TotalMinutes < 16);
    }

    [Fact]
    public void TimeUntilReset_PastResetTime_ReturnsZero()
    {
        // Arrange
        var info = new RateLimitInfo
        {
            ResetTimestamp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds()
        };

        // Act
        var timeUntilReset = info.TimeUntilReset;

        // Assert
        Assert.Equal(TimeSpan.Zero, timeUntilReset);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var info = new RateLimitInfo
        {
            Limit = 150,
            Remaining = 75,
            ResetTimestamp = 1234567890,
            RetryAfter = TimeSpan.FromSeconds(60)
        };

        // Act & Assert
        Assert.Equal(150, info.Limit);
        Assert.Equal(75, info.Remaining);
        Assert.Equal(1234567890, info.ResetTimestamp);
        Assert.Equal(TimeSpan.FromSeconds(60), info.RetryAfter);
    }
}