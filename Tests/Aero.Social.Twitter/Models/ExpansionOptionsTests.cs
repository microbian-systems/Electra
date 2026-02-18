using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Models;

public class ExpansionOptionsTests
{
    [Fact]
    public void ExpansionOptions_ToApiString_WithNone_ReturnsEmptyString()
    {
        // Arrange
        var expansions = ExpansionOptions.None;

        // Act
        var result = expansions.ToApiString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(ExpansionOptions.AuthorId, "author_id")]
    [InlineData(ExpansionOptions.ReferencedTweetsId, "referenced_tweets.id")]
    [InlineData(ExpansionOptions.ReferencedTweetsIdAuthorId, "referenced_tweets.id.author_id")]
    [InlineData(ExpansionOptions.AttachmentsMediaKeys, "attachments.media_keys")]
    [InlineData(ExpansionOptions.AttachmentsPollIds, "attachments.poll_ids")]
    [InlineData(ExpansionOptions.EntitiesMentionsUsername, "entities.mentions.username")]
    [InlineData(ExpansionOptions.EntitiesNoteMentionsUsername, "entities.note.mentions.username")]
    public void ExpansionOptions_ToApiString_IndividualOptions_ReturnCorrectValues(ExpansionOptions expansion, string expected)
    {
        // Act
        var result = expansion.ToApiString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ExpansionOptions_ToApiString_WithMultipleOptions_ReturnsCommaSeparatedString()
    {
        // Arrange
        var expansions = ExpansionOptions.AuthorId | ExpansionOptions.AttachmentsMediaKeys;

        // Act
        var result = expansions.ToApiString();

        // Assert
        Assert.Contains("author_id", result);
        Assert.Contains("attachments.media_keys", result);
        Assert.Contains(",", result);
    }

    [Fact]
    public void ExpansionOptions_ToApiString_WithAllOptions_ReturnsAllExpansionNames()
    {
        // Arrange
        var expansions = ExpansionOptions.All;

        // Act
        var result = expansions.ToApiString();

        // Assert
        Assert.Contains("author_id", result);
        Assert.Contains("referenced_tweets.id", result);
        Assert.Contains("referenced_tweets.id.author_id", result);
        Assert.Contains("attachments.media_keys", result);
        Assert.Contains("attachments.poll_ids", result);
        Assert.Contains("entities.mentions.username", result);
        Assert.Contains("entities.note.mentions.username", result);
    }

    [Fact]
    public void ExpansionOptions_ToApiString_WithComplexCombination_ReturnsCorrectString()
    {
        // Arrange
        var expansions = ExpansionOptions.AuthorId | ExpansionOptions.ReferencedTweetsId | ExpansionOptions.AttachmentsMediaKeys;

        // Act
        var result = expansions.ToApiString();

        // Assert
        Assert.Contains("author_id", result);
        Assert.Contains("referenced_tweets.id", result);
        Assert.Contains("attachments.media_keys", result);
    }
}