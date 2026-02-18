namespace Aero.Social.Models;

/// <summary>
/// Represents a social media integration configuration.
/// </summary>
public class Integration
{
    /// <summary>
    /// Gets or sets the unique identifier for this integration.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the organization ID this integration belongs to.
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider identifier (e.g., "discord", "twitter").
    /// </summary>
    public string ProviderIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the internal ID (user or account ID on the platform).
    /// </summary>
    public string InternalId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth refresh token.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets when the access token expires.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the display name for this integration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile picture URL.
    /// </summary>
    public string? Picture { get; set; }

    /// <summary>
    /// Gets or sets the username/handle on the platform.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets custom instance details (e.g., for self-hosted providers like Lemmy).
    /// </summary>
    public string? CustomInstanceDetails { get; set; }
}
