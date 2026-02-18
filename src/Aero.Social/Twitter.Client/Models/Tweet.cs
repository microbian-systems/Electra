using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Represents a tweet from the Twitter API.
/// </summary>
public class Tweet
{
    /// <summary>
    /// The unique identifier of the tweet.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// The content of the tweet.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// The date and time when the tweet was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The unique identifier of the user who posted the tweet.
    /// </summary>
    [JsonPropertyName("author_id")]
    public string? AuthorId { get; set; }

    /// <summary>
    /// Public engagement metrics for the tweet.
    /// </summary>
    [JsonPropertyName("public_metrics")]
    public PublicMetrics? PublicMetrics { get; set; }
}