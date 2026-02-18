namespace Aero.Social.Models;

/// <summary>
/// Represents media content to be attached to a post.
/// </summary>
public class MediaContent
{
    /// <summary>
    /// Gets or sets the type of media (Image or Video).
    /// </summary>
    public MediaType Type { get; set; }

    /// <summary>
    /// Gets or sets the URL or local path to the media file.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets alternative text for accessibility.
    /// </summary>
    public string? Alt { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail URL for video content.
    /// </summary>
    public string? Thumbnail { get; set; }

    /// <summary>
    /// Gets or sets the timestamp (in seconds) for video thumbnails.
    /// </summary>
    public int? ThumbnailTimestamp { get; set; }
}

/// <summary>
/// Defines the types of media content.
/// </summary>
public enum MediaType
{
    /// <summary>
    /// Image media type.
    /// </summary>
    Image,

    /// <summary>
    /// Video media type.
    /// </summary>
    Video
}
