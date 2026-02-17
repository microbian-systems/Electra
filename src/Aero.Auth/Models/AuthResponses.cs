namespace Aero.Auth.Models;

/// <summary>
/// Response for web-based login (BFF cookie authentication)
/// </summary>
public class LoginWebResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// Response for app-based login (JWT + refresh token)
/// </summary>
public class LoginAppResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int AccessTokenExpiresIn { get; set; } // in seconds
    public string TokenType { get; set; } = "Bearer";
    public string? UserId { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// Response for token refresh
/// </summary>
public class RefreshTokenResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public int AccessTokenExpiresIn { get; set; } // in seconds
}

/// <summary>
/// Response for logout
/// </summary>
public class LogoutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Response for external login challenge (initiates social/passkey flow)
/// </summary>
public class ExternalLoginChallengeResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}
