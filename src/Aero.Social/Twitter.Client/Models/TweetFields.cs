namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Specifies which tweet fields to include in the API response.
/// </summary>
[Flags]
public enum TweetFields
{
    /// <summary>
    /// No additional fields requested.
    /// </summary>
    None = 0,

    /// <summary>
    /// The unique identifier of the user who posted the tweet.
    /// </summary>
    AuthorId = 1 << 0,

    /// <summary>
    /// The UTC datetime that the tweet was created.
    /// </summary>
    CreatedAt = 1 << 1,

    /// <summary>
    /// The text of the tweet.
    /// </summary>
    Text = 1 << 2,

    /// <summary>
    /// Entities that have been parsed out of the text.
    /// </summary>
    Entities = 1 << 3,

    /// <summary>
    /// Geographic location associated with the tweet.
    /// </summary>
    Geo = 1 << 4,

    /// <summary>
    /// Unique identifier of the tweet this tweet is replying to.
    /// </summary>
    InReplyToUserId = 1 << 5,

    /// <summary>
    /// Language of the tweet.
    /// </summary>
    Lang = 1 << 6,

    /// <summary>
    /// Non-public engagement metrics for the tweet.
    /// </summary>
    NonPublicMetrics = 1 << 7,

    /// <summary>
    /// Organic engagement metrics for the tweet.
    /// </summary>
    OrganicMetrics = 1 << 8,

    /// <summary>
    /// Promoted engagement metrics for the tweet.
    /// </summary>
    PromotedMetrics = 1 << 9,

    /// <summary>
    /// Public engagement metrics for the tweet.
    /// </summary>
    PublicMetrics = 1 << 10,

    /// <summary>
    /// Tweets referenced in this tweet.
    /// </summary>
    ReferencedTweets = 1 << 11,

    /// <summary>
    /// Source application used to post the tweet.
    /// </summary>
    Source = 1 << 12,

    /// <summary>
    /// All available tweet fields.
    /// </summary>
    All = AuthorId | CreatedAt | Text | Entities | Geo | InReplyToUserId | Lang | NonPublicMetrics | OrganicMetrics | PromotedMetrics | PublicMetrics | ReferencedTweets | Source
}

/// <summary>
/// Extension methods for TweetFields enum.
/// </summary>
public static class TweetFieldsExtensions
{
    /// <summary>
    /// Converts the TweetFields flags to a comma-separated string for API requests.
    /// </summary>
    public static string ToApiString(this TweetFields fields)
    {
        if (fields == TweetFields.None)
            return string.Empty;

        var parts = new List<string>();

        if (fields.HasFlag(TweetFields.AuthorId))
            parts.Add("author_id");
        if (fields.HasFlag(TweetFields.CreatedAt))
            parts.Add("created_at");
        if (fields.HasFlag(TweetFields.Text))
            parts.Add("text");
        if (fields.HasFlag(TweetFields.Entities))
            parts.Add("entities");
        if (fields.HasFlag(TweetFields.Geo))
            parts.Add("geo");
        if (fields.HasFlag(TweetFields.InReplyToUserId))
            parts.Add("in_reply_to_user_id");
        if (fields.HasFlag(TweetFields.Lang))
            parts.Add("lang");
        if (fields.HasFlag(TweetFields.NonPublicMetrics))
            parts.Add("non_public_metrics");
        if (fields.HasFlag(TweetFields.OrganicMetrics))
            parts.Add("organic_metrics");
        if (fields.HasFlag(TweetFields.PromotedMetrics))
            parts.Add("promoted_metrics");
        if (fields.HasFlag(TweetFields.PublicMetrics))
            parts.Add("public_metrics");
        if (fields.HasFlag(TweetFields.ReferencedTweets))
            parts.Add("referenced_tweets");
        if (fields.HasFlag(TweetFields.Source))
            parts.Add("source");

        return string.Join(",", parts);
    }
}