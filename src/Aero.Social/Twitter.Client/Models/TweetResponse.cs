using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Represents a paginated response from the Twitter API containing tweets.
/// </summary>
public class TweetResponse
{
    /// <summary>
    /// The tweet data returned by the API.
    /// For single tweet requests, this contains one tweet.
    /// For search/timeline requests, this contains multiple tweets.
    /// </summary>
    [JsonPropertyName("data")]
    public List<Tweet>? Data { get; set; }

    /// <summary>
    /// Metadata about the response, including pagination tokens.
    /// </summary>
    [JsonPropertyName("meta")]
    public TweetMeta? Meta { get; set; }

    /// <summary>
    /// Additional data requested via expansions (users, tweets, media, etc.).
    /// </summary>
    [JsonPropertyName("includes")]
    public Includes? Includes { get; set; }
}