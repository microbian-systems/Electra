namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Specifies which expansions to include in the API response.
/// Expansions allow you to request additional data referenced in tweets.
/// </summary>
[System.Flags]
public enum ExpansionOptions
{
    /// <summary>
    /// No expansions requested.
    /// </summary>
    None = 0,

    /// <summary>
    /// Returns a user object representing the author of the tweet.
    /// </summary>
    AuthorId = 1 << 0,

    /// <summary>
    /// Returns a tweet object representing the tweet this tweet is replying to.
    /// </summary>
    ReferencedTweetsId = 1 << 1,

    /// <summary>
    /// Returns a tweet object representing the original tweet that was retweeted.
    /// </summary>
    ReferencedTweetsIdAuthorId = 1 << 2,

    /// <summary>
    /// Returns media objects attached to the tweet.
    /// </summary>
    AttachmentsMediaKeys = 1 << 3,

    /// <summary>
    /// Returns poll objects attached to the tweet.
    /// </summary>
    AttachmentsPollIds = 1 << 4,

    /// <summary>
    /// Returns user objects for users mentioned in the tweet.
    /// </summary>
    EntitiesMentionsUsername = 1 << 5,

    /// <summary>
    /// Returns user objects for users referenced in hashtags.
    /// </summary>
    EntitiesNoteMentionsUsername = 1 << 6,

    /// <summary>
    /// All available expansions.
    /// </summary>
    All = AuthorId | ReferencedTweetsId | ReferencedTweetsIdAuthorId | AttachmentsMediaKeys | AttachmentsPollIds | EntitiesMentionsUsername | EntitiesNoteMentionsUsername
}

/// <summary>
/// Extension methods for ExpansionOptions enum.
/// </summary>
public static class ExpansionOptionsExtensions
{
    /// <summary>
    /// Converts the ExpansionOptions flags to a comma-separated string for API requests.
    /// </summary>
    public static string ToApiString(this ExpansionOptions expansions)
    {
        if (expansions == ExpansionOptions.None)
            return string.Empty;

        var parts = new List<string>();

        if (expansions.HasFlag(ExpansionOptions.AuthorId))
            parts.Add("author_id");
        if (expansions.HasFlag(ExpansionOptions.ReferencedTweetsId))
            parts.Add("referenced_tweets.id");
        if (expansions.HasFlag(ExpansionOptions.ReferencedTweetsIdAuthorId))
            parts.Add("referenced_tweets.id.author_id");
        if (expansions.HasFlag(ExpansionOptions.AttachmentsMediaKeys))
            parts.Add("attachments.media_keys");
        if (expansions.HasFlag(ExpansionOptions.AttachmentsPollIds))
            parts.Add("attachments.poll_ids");
        if (expansions.HasFlag(ExpansionOptions.EntitiesMentionsUsername))
            parts.Add("entities.mentions.username");
        if (expansions.HasFlag(ExpansionOptions.EntitiesNoteMentionsUsername))
            parts.Add("entities.note.mentions.username");

        return string.Join(",", parts);
    }
}