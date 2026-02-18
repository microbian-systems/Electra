using System.Text.Json;
using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class PublicMetricsTests
{
    [Fact]
    public void PublicMetrics_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var metrics = new PublicMetrics();

        // Assert
        Assert.Equal(0, metrics.RetweetCount);
        Assert.Equal(0, metrics.ReplyCount);
        Assert.Equal(0, metrics.LikeCount);
        Assert.Equal(0, metrics.QuoteCount);
    }

    [Fact]
    public void PublicMetrics_Serialization_ShouldIncludeAllProperties()
    {
        // Arrange
        var metrics = new PublicMetrics
        {
            RetweetCount = 100,
            ReplyCount = 25,
            LikeCount = 500,
            QuoteCount = 10
        };

        // Act
        var json = JsonSerializer.Serialize(metrics);

        // Assert
        Assert.Contains("100", json);
        Assert.Contains("25", json);
        Assert.Contains("500", json);
        Assert.Contains("10", json);
    }

    [Fact]
    public void PublicMetrics_Deserialization_ShouldParseAllProperties()
    {
        // Arrange
        var json = @"{
                ""retweet_count"": 100,
                ""reply_count"": 25,
                ""like_count"": 500,
                ""quote_count"": 10
            }";

        // Act
        var metrics = JsonSerializer.Deserialize<PublicMetrics>(json);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(100, metrics.RetweetCount);
        Assert.Equal(25, metrics.ReplyCount);
        Assert.Equal(500, metrics.LikeCount);
        Assert.Equal(10, metrics.QuoteCount);
    }
}