using System.Text.Json;
using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class UserPublicMetricsTests
{
    [Fact]
    public void UserPublicMetrics_DefaultValues_AreZero()
    {
        // Arrange & Act
        var metrics = new UserPublicMetrics();

        // Assert
        Assert.Equal(0, metrics.FollowersCount);
        Assert.Equal(0, metrics.FollowingCount);
        Assert.Equal(0, metrics.TweetCount);
        Assert.Equal(0, metrics.ListedCount);
    }

    [Fact]
    public void UserPublicMetrics_Serialization_ReturnsCorrectJson()
    {
        // Arrange
        var metrics = new UserPublicMetrics
        {
            FollowersCount = 1000,
            FollowingCount = 500,
            TweetCount = 250,
            ListedCount = 50
        };

        // Act
        var json = JsonSerializer.Serialize(metrics);

        // Assert
        Assert.Contains("\"followers_count\":1000", json);
        Assert.Contains("\"following_count\":500", json);
        Assert.Contains("\"tweet_count\":250", json);
        Assert.Contains("\"listed_count\":50", json);
    }

    [Fact]
    public void UserPublicMetrics_Deserialization_PopulatesCorrectly()
    {
        // Arrange
        var json = @"{
                ""followers_count"": 1000,
                ""following_count"": 500,
                ""tweet_count"": 250,
                ""listed_count"": 50
            }";

        // Act
        var metrics = JsonSerializer.Deserialize<UserPublicMetrics>(json);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(1000, metrics.FollowersCount);
        Assert.Equal(500, metrics.FollowingCount);
        Assert.Equal(250, metrics.TweetCount);
        Assert.Equal(50, metrics.ListedCount);
    }

    [Fact]
    public void UserPublicMetrics_Deserialization_WithPartialData_PopulatesCorrectly()
    {
        // Arrange
        var json = @"{
                ""followers_count"": 100,
                ""tweet_count"": 25
            }";

        // Act
        var metrics = JsonSerializer.Deserialize<UserPublicMetrics>(json);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(100, metrics.FollowersCount);
        Assert.Equal(0, metrics.FollowingCount);
        Assert.Equal(25, metrics.TweetCount);
        Assert.Equal(0, metrics.ListedCount);
    }

    [Fact]
    public void UserPublicMetrics_Deserialization_WithZeroValues_HandlesCorrectly()
    {
        // Arrange
        var json = @"{
                ""followers_count"": 0,
                ""following_count"": 0,
                ""tweet_count"": 0,
                ""listed_count"": 0
            }";

        // Act
        var metrics = JsonSerializer.Deserialize<UserPublicMetrics>(json);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.FollowersCount);
        Assert.Equal(0, metrics.FollowingCount);
        Assert.Equal(0, metrics.TweetCount);
        Assert.Equal(0, metrics.ListedCount);
    }
}