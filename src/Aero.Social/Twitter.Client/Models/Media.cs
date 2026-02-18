using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Represents media attached to a tweet.
/// </summary>
public class Media
{
    /// <summary>
    /// The unique identifier of the media.
    /// </summary>
    [JsonPropertyName("media_key")]
    public string MediaKey { get; set; } = null!;

    /// <summary>
    /// The type of media (photo, video, animated_gif).
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The URL to the media file.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// The preview image URL for videos.
    /// </summary>
    [JsonPropertyName("preview_image_url")]
    public string? PreviewImageUrl { get; set; }

    /// <summary>
    /// The width of the media in pixels.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; set; }

    /// <summary>
    /// The height of the media in pixels.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; set; }

    /// <summary>
    /// Alternative text for the media (if provided).
    /// </summary>
    [JsonPropertyName("alt_text")]
    public string? AltText { get; set; }
}