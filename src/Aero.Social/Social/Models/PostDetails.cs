namespace Aero.Social.Models;

/// <summary>
/// Represents the details of a post to be published to a social media platform.
/// </summary>
/// <typeparam name="TSettings">The type of settings specific to the provider.</typeparam>
public class PostDetails<TSettings>
{
    /// <summary>
    /// Gets or sets the unique identifier for this post.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content of the post.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets provider-specific settings for the post.
    /// </summary>
    public TSettings? Settings { get; set; }

    /// <summary>
    /// Gets or sets the media attachments for the post.
    /// </summary>
    public List<MediaContent>? Media { get; set; }

    /// <summary>
    /// Gets or sets the poll details if the post includes a poll.
    /// </summary>
    public PollDetails? Poll { get; set; }
}

/// <summary>
/// Represents the details of a post with dictionary-based settings.
/// </summary>
public class PostDetails : PostDetails<Dictionary<string, object>>
{
}
