using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Metadata about a tweet response, including pagination information.
/// </summary>
public class TweetMeta
{
    /// <summary>
    /// The number of tweets returned in this response.
    /// </summary>
    [JsonPropertyName("result_count")]
    public int ResultCount { get; set; }

    /// <summary>
    /// The next token to use for retrieving the next page of results.
    /// Null if there are no more results.
    /// </summary>
    [JsonPropertyName("next_token")]
    public string? NextToken { get; set; }

    /// <summary>
    /// The previous token to use for retrieving the previous page of results.
    /// Null if this is the first page.
    /// </summary>
    [JsonPropertyName("previous_token")]
    public string? PreviousToken { get; set; }

    /// <summary>
    /// The newest tweet ID in the response.
    /// </summary>
    [JsonPropertyName("newest_id")]
    public string? NewestId { get; set; }

    /// <summary>
    /// The oldest tweet ID in the response.
    /// </summary>
    [JsonPropertyName("oldest_id")]
    public string? OldestId { get; set; }
}