using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class SearchOptionsTests
{
    [Fact]
    public void SearchOptions_DefaultValues_AreNull()
    {
        // Arrange & Act
        var options = new SearchOptions();

        // Assert
        Assert.Null(options.MaxResults);
        Assert.Null(options.SinceId);
        Assert.Null(options.UntilId);
        Assert.Null(options.StartTime);
        Assert.Null(options.EndTime);
        Assert.Null(options.NextToken);
        Assert.Null(options.TweetFields);
        Assert.Null(options.Expansions);
        Assert.Null(options.UserFields);
    }

    [Theory]
    [InlineData(10)]   // Minimum valid
    [InlineData(50)]   // Mid-range
    [InlineData(100)]  // Maximum valid
    public void SearchOptions_Validate_WithValidMaxResults_DoesNotThrow(int maxResults)
    {
        // Arrange
        var options = new SearchOptions { MaxResults = maxResults };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Theory]
    [InlineData(5)]    // Too low
    [InlineData(9)]    // Just below minimum
    [InlineData(101)]  // Just above maximum
    [InlineData(200)]  // Way too high
    public void SearchOptions_Validate_WithInvalidMaxResults_ThrowsArgumentException(int maxResults)
    {
        // Arrange
        var options = new SearchOptions { MaxResults = maxResults };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("MaxResults must be between 10 and 100", exception.Message);
    }

    [Fact]
    public void SearchOptions_Validate_WithValidTimeRange_DoesNotThrow()
    {
        // Arrange
        var options = new SearchOptions
        {
            StartTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero)
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void SearchOptions_Validate_WithStartTimeAfterEndTime_ThrowsArgumentException()
    {
        // Arrange
        var options = new SearchOptions
        {
            StartTime = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("StartTime cannot be greater than EndTime", exception.Message);
    }

    [Fact]
    public void SearchOptions_Validate_WithOnlyStartTime_DoesNotThrow()
    {
        // Arrange
        var options = new SearchOptions
        {
            StartTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void SearchOptions_Validate_WithOnlyEndTime_DoesNotThrow()
    {
        // Arrange
        var options = new SearchOptions
        {
            EndTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero)
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void SearchOptions_Validate_WithSameStartAndEndTime_DoesNotThrow()
    {
        // Arrange
        var sameTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var options = new SearchOptions
        {
            StartTime = sameTime,
            EndTime = sameTime
        };

        // Act & Assert
        options.Validate(); // Should not throw - they're equal, not greater than
    }

    [Fact]
    public void SearchOptions_Validate_WithAllValidProperties_DoesNotThrow()
    {
        // Arrange
        var options = new SearchOptions
        {
            MaxResults = 50,
            SinceId = "1234567890",
            UntilId = "9876543210",
            StartTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero),
            NextToken = "b26v89c19zqg8o3f",
            TweetFields = TweetFields.PublicMetrics | TweetFields.CreatedAt,
            Expansions = ExpansionOptions.AuthorId,
            UserFields = UserFields.PublicMetrics
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void SearchOptions_Validate_WithNullMaxResults_DoesNotThrow()
    {
        // Arrange
        var options = new SearchOptions(); // MaxResults is null by default

        // Act & Assert
        options.Validate(); // Should not throw
    }
}