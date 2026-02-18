using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Contains additional data requested via expansions.
/// </summary>
public class Includes
{
    /// <summary>
    /// Users referenced in the tweets (when author_id expansion is requested).
    /// </summary>
    [JsonPropertyName("users")]
    public List<User>? Users { get; set; }

    /// <summary>
    /// Tweets referenced in the response (when referenced_tweets.id expansion is requested).
    /// </summary>
    [JsonPropertyName("tweets")]
    public List<Tweet>? Tweets { get; set; }

    /// <summary>
    /// Media attached to tweets (when attachments.media_keys expansion is requested).
    /// </summary>
    [JsonPropertyName("media")]
    public List<Media>? Media { get; set; }
}