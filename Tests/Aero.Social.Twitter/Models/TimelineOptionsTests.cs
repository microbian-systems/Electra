using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class TimelineOptionsTests
{
    [Fact]
    public void TimelineOptions_DefaultValues_AreNull()
    {
        // Arrange & Act
        var options = new TimelineOptions();

        // Assert
        Assert.Null(options.MaxResults);
        Assert.Null(options.SinceId);
        Assert.Null(options.UntilId);
        Assert.Null(options.StartTime);
        Assert.Null(options.EndTime);
        Assert.Null(options.PaginationToken);
        Assert.Null(options.Exclude);
        Assert.Null(options.TweetFields);
        Assert.Null(options.Expansions);
        Assert.Null(options.UserFields);
    }

    [Theory]
    [InlineData(5)]    // Minimum valid
    [InlineData(50)]   // Mid-range
    [InlineData(100)]  // Maximum valid
    public void TimelineOptions_Validate_WithValidMaxResults_DoesNotThrow(int maxResults)
    {
        // Arrange
        var options = new TimelineOptions { MaxResults = maxResults };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Theory]
    [InlineData(1)]    // Too low
    [InlineData(4)]    // Just below minimum
    [InlineData(101)]  // Just above maximum
    [InlineData(200)]  // Way too high
    public void TimelineOptions_Validate_WithInvalidMaxResults_ThrowsArgumentException(int maxResults)
    {
        // Arrange
        var options = new TimelineOptions { MaxResults = maxResults };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MaxResults must be between 5 and 100", exception.Message);
    }

    [Fact]
    public void TimelineOptions_Validate_WithValidTimeRange_DoesNotThrow()
    {
        // Arrange
        var options = new TimelineOptions
        {
            StartTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero)
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void TimelineOptions_Validate_WithStartTimeAfterEndTime_ThrowsArgumentException()
    {
        // Arrange
        var options = new TimelineOptions
        {
            StartTime = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("StartTime cannot be greater than EndTime", exception.Message);
    }

    [Fact]
    public void TimelineOptions_Properties_CanBeSet()
    {
        // Arrange & Act
        var options = new TimelineOptions
        {
            MaxResults = 25,
            SinceId = "1234567890",
            UntilId = "9876543210",
            StartTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero),
            PaginationToken = "test_token",
            Exclude = "retweets,replies",
            TweetFields = TweetFields.PublicMetrics,
            Expansions = ExpansionOptions.AuthorId,
            UserFields = UserFields.PublicMetrics
        };

        // Assert
        Assert.Equal(25, options.MaxResults);
        Assert.Equal("1234567890", options.SinceId);
        Assert.Equal("9876543210", options.UntilId);
        Assert.NotNull(options.StartTime);
        Assert.NotNull(options.EndTime);
        Assert.Equal("test_token", options.PaginationToken);
        Assert.Equal("retweets,replies", options.Exclude);
        Assert.NotNull(options.TweetFields);
        Assert.NotNull(options.Expansions);
        Assert.NotNull(options.UserFields);
    }
}