using System.Security.Cryptography;
using System.Text;
using Electra.Auth.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Electra.Auth.Services;

/// <summary>
/// Production-grade JWT signing key store with rotation support.
/// Keys are persisted in the database for durability and can be rotated without downtime.
/// </summary>
public class JwtSigningKeyStore : IJwtSigningKeyStore
{
    private readonly IDbContextFactory<DbContext> _contextFactory;
    private readonly ILogger<JwtSigningKeyStore> _logger;
    private readonly IMemoryCache _cache;
    private const string CurrentKeyIdCacheKey = "jwt:current_key_id";
    private const string AllKeysCacheKey = "jwt:all_keys";
    private const int CacheDurationMinutes = 5;

    public JwtSigningKeyStore(
        IDbContextFactory<DbContext> contextFactory,
        ILogger<JwtSigningKeyStore> logger,
        IMemoryCache cache)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _cache = cache;
    }

    public async Task<SecurityKey> GetCurrentSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        var keyId = await GetCurrentKeyIdAsync(cancellationToken);
        var key = await GetKeyByIdAsync(keyId, cancellationToken);

        if (key == null)
        {
            throw new InvalidOperationException($"Current signing key not found: {keyId}");
        }

        return key;
    }

    public async Task<string> GetCurrentKeyIdAsync(CancellationToken cancellationToken = default)
    {
        // Try cache first
        if (_cache.TryGetValue(CurrentKeyIdCacheKey, out string cachedKeyId))
        {
            return cachedKeyId;
        }

        // Query database
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var currentKey = await context.Set<JwtSigningKey>()
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.IsCurrentSigningKey && k.IsValid, cancellationToken);

        if (currentKey == null)
        {
            throw new InvalidOperationException("No current signing key found. Initialize keys first.");
        }

        // Cache result
        _cache.Set(CurrentKeyIdCacheKey, currentKey.KeyId, TimeSpan.FromMinutes(CacheDurationMinutes));

        return currentKey.KeyId;
    }

    public async Task<IEnumerable<SecurityKey>> GetValidationKeysAsync(CancellationToken cancellationToken = default)
    {
        // Try cache first
        if (_cache.TryGetValue(AllKeysCacheKey, out IEnumerable<SecurityKey> cachedKeys))
        {
            return cachedKeys;
        }

        // Query database
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var keys = await context.Set<JwtSigningKey>()
            .AsNoTracking()
            .Where(k => k.IsValid)
            .ToListAsync(cancellationToken);

        var securityKeys = keys
            .Select(k => new SymmetricSecurityKey(Convert.FromBase64String(k.KeyMaterial)))
            .Cast<SecurityKey>()
            .ToList();

        // Cache result
        _cache.Set(AllKeysCacheKey, securityKeys, TimeSpan.FromMinutes(CacheDurationMinutes));

        return securityKeys;
    }

    public async Task<SigningCredentials> GetSigningCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var key = await GetCurrentSigningKeyAsync(cancellationToken);
        return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public async Task<string> RotateSigningKeyAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Mark current signing key as no longer current
        var currentKey = await context.Set<JwtSigningKey>()
            .FirstOrDefaultAsync(k => k.IsCurrentSigningKey && k.IsValid, cancellationToken);

        if (currentKey != null)
        {
            currentKey.IsCurrentSigningKey = false;
            _logger.LogInformation("Rotated out key: {KeyId}", currentKey.KeyId);
        }

        // Create new signing key
        var newKey = new JwtSigningKey
        {
            Id = Guid.NewGuid().ToString(),
            KeyId = Guid.NewGuid().ToString("N").Substring(0, 16),
            KeyMaterial = Convert.ToBase64String(GenerateRandomKey(32)),
            CreatedOn = DateTimeOffset.UtcNow,
            IsCurrentSigningKey = true,
            Algorithm = "HS256"
        };

        context.Set<JwtSigningKey>().Add(newKey);
        await context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cache.Remove(CurrentKeyIdCacheKey);
        _cache.Remove(AllKeysCacheKey);

        _logger.LogInformation("Created new signing key: {KeyId}", newKey.KeyId);

        return newKey.KeyId;
    }

    public async Task RevokeKeyAsync(string keyId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var key = await context.Set<JwtSigningKey>()
            .FirstOrDefaultAsync(k => k.KeyId == keyId, cancellationToken);

        if (key != null)
        {
            key.RevokedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            _cache.Remove(AllKeysCacheKey);
            _logger.LogInformation("Revoked key: {KeyId}", keyId);
        }
    }

    public async Task<SecurityKey?> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var key = await context.Set<JwtSigningKey>()
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyId == keyId && k.IsValid, cancellationToken);

        if (key == null)
        {
            return null;
        }

        return new SymmetricSecurityKey(Convert.FromBase64String(key.KeyMaterial));
    }

    private static byte[] GenerateRandomKey(int length)
    {
        using var rng = new RNGCryptoServiceProvider();
        var key = new byte[length];
        rng.GetBytes(key);
        return key;
    }
}
