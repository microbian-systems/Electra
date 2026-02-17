namespace Aero.Auth.Services;

/// <summary>
/// Production-grade JWT token service for generating and validating access tokens.
/// Uses the configured signing key store for key rotation support.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IJwtSigningKeyStore _signingKeyStore;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly IConfiguration _config;

    public int AccessTokenLifetime { get; }

    public JwtTokenService(
        IJwtSigningKeyStore signingKeyStore,
        ILogger<JwtTokenService> logger,
        IConfiguration config)
    {
        _signingKeyStore = signingKeyStore;
        _logger = logger;
        _config = config;
        AccessTokenLifetime = _config.GetValue("Auth:AccessTokenLifetimeSeconds", 300); // 5 minutes
    }

    public async Task<string> GenerateAccessTokenAsync(
        string userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var signingCredentials = await _signingKeyStore.GetSigningCredentialsAsync(cancellationToken);
        var keyId = await _signingKeyStore.GetCurrentKeyIdAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _config["Auth:Jwt:Issuer"] ?? "Aero",
            audience: _config["Auth:Jwt:Audience"] ?? "AeroClients",
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(AccessTokenLifetime),
            signingCredentials: signingCredentials);

        // Add kid header for key rotation support
        token.Header["kid"] = keyId;

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwt = tokenHandler.WriteToken(token);

        _logger.LogDebug("Generated access token for user {UserId}", userId);

        return jwt;
    }

    public async Task<(bool IsValid, ClaimsPrincipal? Principal)> ValidateAccessTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationKeys = await _signingKeyStore.GetValidationKeysAsync(cancellationToken);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = validationKeys,
                ValidateIssuer = true,
                ValidIssuer = _config["Auth:Jwt:Issuer"] ?? "Aero",
                ValidateAudience = true,
                ValidAudience = _config["Auth:Jwt:Audience"] ?? "AeroClients",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                _logger.LogDebug("Validated access token for user {UserId}", userId);
            }

            return (true, principal);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating token");
            return (false, null);
        }
    }
}
