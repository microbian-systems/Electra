using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System.Security.Cryptography;

namespace Electra.Auth.Services;

/// <summary>
/// Production-grade refresh token service with rotation and revocation support.
/// All tokens are stored as SHA-256 hashes for security (plaintext never stored).
/// Supports token rotation with one-time use enforcement.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly IAsyncDocumentSession session;
    readonly ILogger<RefreshTokenService> logger;
    readonly IConfiguration config;
    readonly int refreshTokenLifetimeDays;

    public RefreshTokenService(
        IAsyncDocumentSession session,
        ILogger<RefreshTokenService> logger,
        IConfiguration config)
    {
        this.session = session;
        this.logger = logger;
        this.config = config;
        refreshTokenLifetimeDays = config.GetValue("Auth:RefreshTokenLifetimeDays", 30);
    }

    public async Task<string> GenerateRefreshTokenAsync(
        string userId,
        string clientType,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var token = GenerateRandomToken(64);
        var tokenHash = HashToken(token);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedOn = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshTokenLifetimeDays),
            ClientType = clientType,
            IssuedFromIpAddress = ipAddress,
            UserAgent = userAgent
        };

        
        await session.StoreAsync(refreshToken);
        await session.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Generated refresh token for user {UserId} from {IpAddress} ({ClientType})",
            userId, ipAddress ?? "unknown", clientType);

        return token;
    }

    public async Task<string?> ValidateRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            logger.LogWarning("Empty refresh token provided");
            return null;
        }

        var tokenHash = HashToken(token);

        var refreshToken = await session.Query<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (refreshToken == null)
        {
            logger.LogWarning("Refresh token not found (invalid or already rotated)");
            return null;
        }

        if (!refreshToken.IsActive)
        {
            logger.LogWarning("Refresh token is not active for user {UserId}", refreshToken.UserId);
            return null;
        }

        return refreshToken.UserId;
    }

    public async Task<string> RotateRefreshTokenAsync(
        string oldToken,
        string clientType,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        // Validate and get user ID from old token
        var userId = await ValidateRefreshTokenAsync(oldToken, cancellationToken);
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("Invalid refresh token");
        }

        // Mark old token as rotated
        var oldTokenHash = HashToken(oldToken);
        

        var oldTokenRecord = await session.Query<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.TokenHash == oldTokenHash, cancellationToken);

        if (oldTokenRecord != null)
        {
            var newToken = GenerateRandomToken(64);
            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                TokenHash = HashToken(newToken),
                CreatedOn = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshTokenLifetimeDays),
                ClientType = clientType,
                IssuedFromIpAddress = ipAddress,
                UserAgent = userAgent,
                ReplacedByTokenId = oldTokenRecord.Id
            };

            oldTokenRecord.ReplacedByTokenId = newRefreshToken.Id;
            await session.StoreAsync(newRefreshToken);
            await session.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Rotated refresh token for user {UserId}",
                userId);

            return newToken;
        }

        throw new InvalidOperationException("Old token record not found");
    }

    public async Task RevokeRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(token);

        
        var refreshToken = await session.Query<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (refreshToken != null)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            await session.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Revoked refresh token for user {UserId}", refreshToken.UserId);
        }
    }

    public async Task RevokeAllUserTokensAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        

        var activeTokens = await session.Query<RefreshToken>()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        if (activeTokens.Count > 0)
        {
            await session.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Revoked all refresh tokens for user {UserId} ({Count} tokens)", userId, activeTokens.Count);
        }
    }

    public async Task<IEnumerable<(string Id, string ClientType, DateTimeOffset CreatedAt, string? IpAddress)>> GetActiveTokensAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {

        var tokens = await session.Query<RefreshToken>()
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .Select(rt => new { rt.Id, rt.ClientType, rt.CreatedOn, rt.IssuedFromIpAddress })
            .ToListAsync(cancellationToken);

        return tokens.Select(t => (t.Id, t.ClientType, t.CreatedOn, t.IssuedFromIpAddress));
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }

    private static string GenerateRandomToken(uint length)
    {
        var randomBytes = new byte[length];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
