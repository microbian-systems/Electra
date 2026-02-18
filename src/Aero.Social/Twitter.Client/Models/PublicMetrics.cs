using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Represents public engagement metrics for a tweet.
/// </summary>
public class PublicMetrics
{
    /// <summary>
    /// Number of times this tweet has been retweeted.
    /// </summary>
    [JsonPropertyName("retweet_count")]
    public int RetweetCount { get; set; }

    /// <summary>
    /// Number of replies to this tweet.
    /// </summary>
    [JsonPropertyName("reply_count")]
    public int ReplyCount { get; set; }

    /// <summary>
    /// Number of likes for this tweet.
    /// </summary>
    [JsonPropertyName("like_count")]
    public int LikeCount { get; set; }

    /// <summary>
    /// Number of times this tweet has been quoted.
    /// </summary>
    [JsonPropertyName("quote_count")]
    public int QuoteCount { get; set; }
}