using System.ComponentModel.DataAnnotations;
using Electra.Core.Entities;

namespace Electra.Models.Entities;

/// <summary>
/// Represents a refresh token used for session management.
/// Supports both web (BFF) and app (MAUI) clients.
/// </summary>
public class RefreshToken : IEntity<string>
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Reference to the ElectraUser this token belongs to
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the actual token (never store plaintext)
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedOn { get; set; }
    public string CreatedBy { get; set; } = "system";
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the token expires (absolute expiration)
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// When the token was revoked (if null, token is active)
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Optional: the ID of the token that replaced this one (for rotation tracking)
    /// </summary>
    public string? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Client identifier (web, mobile, desktop)
    /// </summary>
    public string ClientType { get; set; } = "web";

    /// <summary>
    /// IP address where token was issued (for security audit)
    /// </summary>
    public string? IssuedFromIpAddress { get; set; }

    /// <summary>
    /// User agent of the client that issued the token
    /// </summary>
    public string? UserAgent { get; set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;
}

