namespace Aero.Social.Twitter.Client.Errors;

/// <summary>
/// Represents an error returned by the Twitter API.
/// </summary>
public class TwitterError
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field that caused the error, if applicable.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the resource ID associated with the error, if applicable.
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the resource type associated with the error, if applicable.
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the API documentation URL for this error.
    /// </summary>
    public string? DocumentationUrl { get; set; }
}