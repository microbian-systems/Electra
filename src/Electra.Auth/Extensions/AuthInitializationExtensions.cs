using Electra.Auth.Models;
using Electra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Electra.Auth.Extensions;

/// <summary>
/// Helper to initialize authentication infrastructure on app startup
/// </summary>
public static class AuthInitializationExtensions
{
    /// <summary>
    /// Initializes JWT signing keys if they don't exist
    /// Should be called during app startup
    /// </summary>
    public static async Task InitializeJwtSigningKeysAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ElectraDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ElectraDbContext>>();

        // Check if any signing keys exist
        var existingKeys = await context.Set<JwtSigningKey>()
            .CountAsync(cancellationToken);

        if (existingKeys == 0)
        {
            logger.LogInformation("Initializing JWT signing keys...");

            var initialKey = new JwtSigningKey
            {
                Id = Guid.NewGuid().ToString(),
                KeyId = Guid.NewGuid().ToString("N").Substring(0, 16),
                KeyMaterial = Convert.ToBase64String(GenerateRandomKey(32)),
                CreatedOn = DateTimeOffset.UtcNow,
                IsCurrentSigningKey = true,
                Algorithm = "HS256"
            };

            context.Set<JwtSigningKey>().Add(initialKey);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("JWT signing key initialized: {KeyId}", initialKey.KeyId);
        }
    }

    private static byte[] GenerateRandomKey(int length)
    {
        using var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
        var key = new byte[length];
        rng.GetBytes(key);
        return key;
    }
}
