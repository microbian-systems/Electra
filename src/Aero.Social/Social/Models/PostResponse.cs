namespace Aero.Social.Models;

/// <summary>
/// Represents the response from a post operation.
/// </summary>
public class PostResponse
{
    /// <summary>
    /// Gets or sets the original post ID from the request.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID assigned by the social media platform.
    /// </summary>
    public string PostId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL where the published post can be viewed.
    /// </summary>
    public string ReleaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the post operation (e.g., "completed", "posted", "error").
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
