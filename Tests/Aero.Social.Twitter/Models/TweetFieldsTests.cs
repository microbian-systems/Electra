using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class TweetFieldsTests
{
    [Fact]
    public void TweetFields_ToApiString_WithNone_ReturnsEmptyString()
    {
        // Arrange
        var fields = TweetFields.None;

        // Act
        var result = fields.ToApiString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(TweetFields.AuthorId, "author_id")]
    [InlineData(TweetFields.CreatedAt, "created_at")]
    [InlineData(TweetFields.Text, "text")]
    [InlineData(TweetFields.Entities, "entities")]
    [InlineData(TweetFields.Geo, "geo")]
    [InlineData(TweetFields.InReplyToUserId, "in_reply_to_user_id")]
    [InlineData(TweetFields.Lang, "lang")]
    [InlineData(TweetFields.NonPublicMetrics, "non_public_metrics")]
    [InlineData(TweetFields.OrganicMetrics, "organic_metrics")]
    [InlineData(TweetFields.PromotedMetrics, "promoted_metrics")]
    [InlineData(TweetFields.PublicMetrics, "public_metrics")]
    [InlineData(TweetFields.ReferencedTweets, "referenced_tweets")]
    [InlineData(TweetFields.Source, "source")]
    public void TweetFields_ToApiString_IndividualFields_ReturnCorrectValues(TweetFields field, string expected)
    {
        // Act
        var result = field.ToApiString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TweetFields_ToApiString_WithMultipleFields_ReturnsCommaSeparatedString()
    {
        // Arrange
        var fields = TweetFields.AuthorId | TweetFields.CreatedAt | TweetFields.Text;

        // Act
        var result = fields.ToApiString();

        // Assert
        Assert.Contains("author_id", result);
        Assert.Contains("created_at", result);
        Assert.Contains("text", result);
        Assert.Contains(",", result);
    }

    [Fact]
    public void TweetFields_ToApiString_WithAllFields_ReturnsAllFieldNames()
    {
        // Arrange
        var fields = TweetFields.All;

        // Act
        var result = fields.ToApiString();

        // Assert
        Assert.Contains("author_id", result);
        Assert.Contains("created_at", result);
        Assert.Contains("text", result);
        Assert.Contains("entities", result);
        Assert.Contains("geo", result);
        Assert.Contains("in_reply_to_user_id", result);
        Assert.Contains("lang", result);
        Assert.Contains("non_public_metrics", result);
        Assert.Contains("organic_metrics", result);
        Assert.Contains("promoted_metrics", result);
        Assert.Contains("public_metrics", result);
        Assert.Contains("referenced_tweets", result);
        Assert.Contains("source", result);
    }

    [Fact]
    public void TweetFields_ToApiString_WithComplexCombination_ReturnsCorrectString()
    {
        // Arrange
        var fields = TweetFields.AuthorId | TweetFields.PublicMetrics | TweetFields.ReferencedTweets;

        // Act
        var result = fields.ToApiString();

        // Assert
        Assert.Contains("author_id", result);
        Assert.Contains("public_metrics", result);
        Assert.Contains("referenced_tweets", result);
    }
}