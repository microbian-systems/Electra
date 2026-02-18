namespace Aero.Social.Models;

/// <summary>
/// Contains the authentication token details returned from a successful OAuth flow.
/// </summary>
public class AuthTokenDetails
{
    /// <summary>
    /// Gets or sets the user's unique identifier on the platform.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth refresh token (if applicable).
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the token expiration time in seconds.
    /// </summary>
    public int? ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the URL of the user's profile picture.
    /// </summary>
    public string? Picture { get; set; }

    /// <summary>
    /// Gets or sets the user's username/handle.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional settings required by the provider.
    /// </summary>
    public List<AdditionalSetting>? AdditionalSettings { get; set; }
}

/// <summary>
/// Represents an additional setting for a provider integration.
/// </summary>
public class AdditionalSetting
{
    /// <summary>
    /// Gets or sets the title/label for this setting.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of this setting.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of input for this setting.
    /// </summary>
    public AdditionalSettingType Type { get; set; }

    /// <summary>
    /// Gets or sets the current value of this setting.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets a validation regex pattern for this setting.
    /// </summary>
    public string? Regex { get; set; }
}

/// <summary>
/// Defines the types of additional setting inputs.
/// </summary>
public enum AdditionalSettingType
{
    /// <summary>
    /// A checkbox input for boolean values.
    /// </summary>
    Checkbox,

    /// <summary>
    /// A single-line text input.
    /// </summary>
    Text,

    /// <summary>
    /// A multi-line textarea input.
    /// </summary>
    Textarea
}
