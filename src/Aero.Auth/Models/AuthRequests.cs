using System.ComponentModel.DataAnnotations;

namespace Aero.Auth.Models;

/// <summary>
/// Request for web-based login (BFF)
/// </summary>
public class LoginWebRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Request for app-based login (MAUI with JWT)
/// </summary>
public class LoginAppRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Client identifier (e.g., "mobile", "desktop")
    /// </summary>
    public string ClientType { get; set; } = "mobile";

    /// <summary>
    /// Device identifier for per-device refresh tokens (future use)
    /// </summary>
    public string? DeviceId { get; set; }
}

/// <summary>
/// Request to refresh an access token
/// </summary>
public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Request to initiate external login (social or passkey)
/// </summary>
public class ExternalLoginChallengeRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Where to redirect after callback (web only)
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Client type for routing (web vs app callback)
    /// </summary>
    public string ClientType { get; set; } = "web";
}
