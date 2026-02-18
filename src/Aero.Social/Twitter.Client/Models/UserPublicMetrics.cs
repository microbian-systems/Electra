using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Represents public engagement metrics for a Twitter user.
/// </summary>
public class UserPublicMetrics
{
    /// <summary>
    /// Number of followers this user has.
    /// </summary>
    [JsonPropertyName("followers_count")]
    public int FollowersCount { get; set; }

    /// <summary>
    /// Number of users this user is following.
    /// </summary>
    [JsonPropertyName("following_count")]
    public int FollowingCount { get; set; }

    /// <summary>
    /// Number of tweets (including retweets) posted by this user.
    /// </summary>
    [JsonPropertyName("tweet_count")]
    public int TweetCount { get; set; }

    /// <summary>
    /// Number of lists this user is a member of.
    /// </summary>
    [JsonPropertyName("listed_count")]
    public int ListedCount { get; set; }
}