using System.Text.Json;
using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class TweetTests
{
    [Fact]
    public void Tweet_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var tweet = new Tweet
        {
            Id = "1234567890",
            Text = "Hello, Twitter!",
            CreatedAt = DateTimeOffset.UtcNow,
            AuthorId = "9876543210"
        };

        // Assert
        Assert.NotNull(tweet.Id);
        Assert.NotNull(tweet.Text);
        Assert.NotEqual(default(DateTimeOffset), tweet.CreatedAt);
    }

    [Fact]
    public void Tweet_Serialization_ShouldIncludeAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.Parse("2024-01-15T10:30:00+00:00");
        var tweet = new Tweet
        {
            Id = "1234567890",
            Text = "Hello, Twitter!",
            CreatedAt = createdAt,
            AuthorId = "9876543210",
            PublicMetrics = new PublicMetrics
            {
                RetweetCount = 10,
                ReplyCount = 5,
                LikeCount = 50,
                QuoteCount = 2
            }
        };

        // Act
        var json = JsonSerializer.Serialize(tweet);

        // Assert
        Assert.Contains("1234567890", json);
        Assert.Contains("Hello, Twitter!", json);
        Assert.Contains("9876543210", json);
    }

    [Fact]
    public void Tweet_Deserialization_ShouldParseAllProperties()
    {
        // Arrange
        var json = @"{
                ""id"": ""1234567890"",
                ""text"": ""Hello, Twitter!"",
                ""created_at"": ""2024-01-15T10:30:00.000Z"",
                ""author_id"": ""9876543210"",
                ""public_metrics"": {
                    ""retweet_count"": 10,
                    ""reply_count"": 5,
                    ""like_count"": 50,
                    ""quote_count"": 2
                }
            }";

        // Act
        var tweet = JsonSerializer.Deserialize<Tweet>(json);

        // Assert
        Assert.NotNull(tweet);
        Assert.Equal("1234567890", tweet.Id);
        Assert.Equal("Hello, Twitter!", tweet.Text);
        Assert.Equal("9876543210", tweet.AuthorId);
        Assert.NotNull(tweet.PublicMetrics);
        Assert.Equal(10, tweet.PublicMetrics.RetweetCount);
        Assert.Equal(5, tweet.PublicMetrics.ReplyCount);
        Assert.Equal(50, tweet.PublicMetrics.LikeCount);
        Assert.Equal(2, tweet.PublicMetrics.QuoteCount);
    }

    [Fact]
    public void Tweet_Deserialization_ShouldHandleNullableFields()
    {
        // Arrange
        var json = @"{
                ""id"": ""1234567890"",
                ""text"": ""Just a simple tweet"",
                ""created_at"": ""2024-01-15T10:30:00.000Z""
            }";

        // Act
        var tweet = JsonSerializer.Deserialize<Tweet>(json);

        // Assert
        Assert.NotNull(tweet);
        Assert.Equal("1234567890", tweet.Id);
        Assert.Null(tweet.AuthorId);
        Assert.Null(tweet.PublicMetrics);
    }
}