namespace Aero.Auth.Services;

/// <summary>
/// Abstraction for JWT signing key persistence.
/// Allows switching between RavenDB, Entity Framework, or other persistence providers.
/// </summary>
public interface IJwtSigningKeyPersistence
{
    /// <summary>
    /// Gets the current signing key marked as active.
    /// </summary>
    Task<JwtSigningKey?> GetCurrentSigningKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all valid (non-revoked) signing keys.
    /// </summary>
    Task<IEnumerable<JwtSigningKey>> GetValidSigningKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific signing key by its key ID.
    /// </summary>
    Task<JwtSigningKey?> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new signing key to the store.
    /// </summary>
    Task<bool> AddKeyAsync(JwtSigningKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing signing key.
    /// </summary>
    Task<bool> UpdateKeyAsync(JwtSigningKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the current signing key as inactive.
    /// </summary>
    Task<bool> DeactivateCurrentKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a signing key by its ID.
    /// </summary>
    Task<bool> RevokeKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists any pending changes to the data store.
    /// </summary>
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
