using System;
using System.ComponentModel.DataAnnotations;
using Electra.Core.Entities;

namespace Electra.Models.Entities;

/// <summary>
/// Represents a JWT signing key with rotation support.
/// Allows multiple keys to be active for validation while only one is used for signing.
/// </summary>
public class JwtSigningKey : IEntity<string>
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Key identifier (kid) used in JWT headers
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// The actual signing key (base64 encoded)
    /// </summary>
    public string KeyMaterial { get; set; } = string.Empty;

    /// <summary>
    /// When this key was created
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ModifiedOn { get; set; }
    public string CreatedBy { get; set; } = "system";
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>
    /// When this key should no longer be used for validation
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Whether this is the current key used for signing new tokens
    /// Only one key should have this true at any time
    /// </summary>
    public bool IsCurrentSigningKey { get; set; }

    /// <summary>
    /// Algorithm used for signing (e.g., "HS256")
    /// </summary>
    public string Algorithm { get; set; } = "HS256";

    /// <summary>
    /// Determines if this key can be used for validation
    /// </summary>
    public bool IsValid => RevokedAt == null;
}
