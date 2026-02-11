using Electra.Core.Entities;

namespace Electra.Models.Entities;

/// <summary>
/// Represents a refresh token used for session management.
/// Supports both web (BFF) and app (MAUI) clients.
/// 
/// RavenDB Design: This document stores only the UserId reference (not the full user object).
/// Query by UserId to find all tokens for a user, or by TokenHash for validation.
/// </summary>
public class RefreshToken : Entity
{
    /// <summary>
    /// Reference to the ElectraUser ID this token belongs to.
    /// In RavenDB, store document IDs as strings, not nested objects.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the actual token (never store plaintext)
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

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

    /// <summary>
    /// Gets a value indicating whether the item is currently active.
    /// </summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;
}

