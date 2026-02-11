namespace Electra.Auth.Services;

/// <summary>
/// Abstraction for JWT signing key management with support for key rotation.
/// </summary>
public interface IJwtSigningKeyStore
{
    /// <summary>
    /// Gets the current signing key used to sign new tokens
    /// </summary>
    Task<SecurityKey> GetCurrentSigningKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the key identifier (kid) for the current signing key
    /// </summary>
    Task<string> GetCurrentKeyIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all valid (non-revoked) keys used for validating tokens
    /// </summary>
    Task<IEnumerable<SecurityKey>> GetValidationKeysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the signing credentials (key + algorithm) for the current key
    /// </summary>
    Task<SigningCredentials> GetSigningCredentialsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the signing key - marks current as old and creates new signing key
    /// </summary>
    Task<string> RotateSigningKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific key by ID
    /// </summary>
    Task RevokeKeyAsync(string keyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a key by its ID (for validation based on JWT kid header)
    /// </summary>
    Task<SecurityKey?> GetKeyByIdAsync(string keyId, CancellationToken cancellationToken = default);
}
