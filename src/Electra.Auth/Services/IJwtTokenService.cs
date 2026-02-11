namespace Electra.Auth.Services;

/// <summary>
/// Core JWT token generation and validation service
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a short-lived access token for a user
    /// </summary>
    Task<string> GenerateAccessTokenAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an access token and returns claims if valid
    /// </summary>
    Task<(bool IsValid, System.Security.Claims.ClaimsPrincipal? Principal)> ValidateAccessTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the lifetime of access tokens (in seconds)
    /// </summary>
    int AccessTokenLifetime { get; }
}
