using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Aero.Auth.Services;
using Aero.Models.Entities;

namespace Aero.Auth.Tests.Services;

/// <summary>
/// Integration tests for JWT signing key store with mocked persistence layer.
/// Tests the key store behavior using the abstracted persistence interface.
/// </summary>
public class JwtSigningKeyStoreIntegrationTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<JwtSigningKeyStore> _mockLogger;
    private readonly IJwtSigningKeyPersistence _mockPersistence;

    public JwtSigningKeyStoreIntegrationTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = Substitute.For<ILogger<JwtSigningKeyStore>>();
        _mockPersistence = Substitute.For<IJwtSigningKeyPersistence>();
    }

    #region GetCurrentSigningKey Tests

    [Fact]
    public void GetCurrentSigningKeyAsync_WithValidKey_ShouldReturnSecurityKey()
    {
        // Arrange
        var testKey = new JwtSigningKey
        {
            Id = "test-id",
            KeyId = "key-1",
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = true,
            CreatedOn = DateTimeOffset.UtcNow
        };

        _mockPersistence.GetCurrentSigningKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((JwtSigningKey?)testKey));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act & Assert
        store.GetCurrentSigningKeyAsync().Result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentSigningKeyAsync_WithNullKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockPersistence.GetCurrentSigningKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((JwtSigningKey?)null));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        Func<Task> act = () => store.GetCurrentSigningKeyAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No current signing key found*");
    }

    #endregion

    #region GetCurrentKeyId Tests

    [Fact]
    public async Task GetCurrentKeyIdAsync_WithValidKey_ShouldReturnKeyId()
    {
        // Arrange
        const string expectedKeyId = "key-abc123";
        var testKey = new JwtSigningKey
        {
            Id = "test-id",
            KeyId = expectedKeyId,
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = true
        };

        _mockPersistence.GetCurrentSigningKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((JwtSigningKey?)testKey));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        var result = await store.GetCurrentKeyIdAsync();

        // Assert
        result.Should().Be(expectedKeyId);
    }

    [Fact]
    public async Task GetCurrentKeyIdAsync_ShouldCacheResult()
    {
        // Arrange
        const string expectedKeyId = "key-abc123";
        var testKey = new JwtSigningKey
        {
            Id = "test-id",
            KeyId = expectedKeyId,
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = true
        };

        _mockPersistence.GetCurrentSigningKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((JwtSigningKey?)testKey));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        var result1 = await store.GetCurrentKeyIdAsync();
        var result2 = await store.GetCurrentKeyIdAsync();

        // Assert
        result1.Should().Be(result2);
        // Verify persistence was only called once due to caching
        await _mockPersistence.Received(1).GetCurrentSigningKeyAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetValidationKeys Tests

    [Fact]
    public async Task GetValidationKeysAsync_WithMultipleKeys_ShouldReturnAllKeys()
    {
        // Arrange
        var testKeys = new[]
        {
            new JwtSigningKey
            {
                Id = "test-id-1",
                KeyId = "key-1",
                KeyMaterial = Convert.ToBase64String(new byte[32]),
                IsCurrentSigningKey = true
            },
            new JwtSigningKey
            {
                Id = "test-id-2",
                KeyId = "key-2",
                KeyMaterial = Convert.ToBase64String(new byte[32]),
                IsCurrentSigningKey = false
            }
        };

        _mockPersistence.GetValidSigningKeysAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IEnumerable<JwtSigningKey>)testKeys));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        var result = await store.GetValidationKeysAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(2);
        result.Should().AllBeOfType<SymmetricSecurityKey>();
    }

    [Fact]
    public async Task GetValidationKeysAsync_ShouldCacheResults()
    {
        // Arrange
        var testKeys = new[]
        {
            new JwtSigningKey
            {
                Id = "test-id-1",
                KeyId = "key-1",
                KeyMaterial = Convert.ToBase64String(new byte[32])
            }
        };

        _mockPersistence.GetValidSigningKeysAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IEnumerable<JwtSigningKey>)testKeys));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        var result1 = await store.GetValidationKeysAsync();
        var result2 = await store.GetValidationKeysAsync();

        // Assert
        result1.Should().HaveCount(result2.Count());
        // Verify persistence was only called once due to caching
        await _mockPersistence.Received(1).GetValidSigningKeysAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region RotateSigningKey Tests

    [Fact]
    public async Task RotateSigningKeyAsync_ShouldCreateNewKey()
    {
        // Arrange
        _mockPersistence.DeactivateCurrentKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _mockPersistence.AddKeyAsync(Arg.Any<JwtSigningKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _mockPersistence.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        var newKeyId = await store.RotateSigningKeyAsync();

        // Assert
        newKeyId.Should().NotBeNullOrEmpty();
        await _mockPersistence.Received(1).DeactivateCurrentKeyAsync(Arg.Any<CancellationToken>());
        await _mockPersistence.Received(1).AddKeyAsync(Arg.Any<JwtSigningKey>(), Arg.Any<CancellationToken>());
        await _mockPersistence.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RotateSigningKeyAsync_ShouldInvalidateCache()
    {
        // Arrange
        const string originalKeyId = "key-original";
        _memoryCache.Set("jwt:current_key_id", originalKeyId);
        _memoryCache.Set("jwt:all_keys", new List<SecurityKey>());

        _mockPersistence.DeactivateCurrentKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _mockPersistence.AddKeyAsync(Arg.Any<JwtSigningKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _mockPersistence.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        await store.RotateSigningKeyAsync();

        // Assert
        _memoryCache.TryGetValue("jwt:current_key_id", out _).Should().BeFalse();
        _memoryCache.TryGetValue("jwt:all_keys", out _).Should().BeFalse();
    }

    #endregion

    #region RevokeKey Tests

    [Fact]
    public async Task RevokeKeyAsync_WithValidKeyId_ShouldRevokeKey()
    {
        // Arrange
        const string keyIdToRevoke = "key-to-revoke";

        _mockPersistence.RevokeKeyAsync(keyIdToRevoke, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _mockPersistence.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        await store.RevokeKeyAsync(keyIdToRevoke);

        // Assert
        await _mockPersistence.Received(1).RevokeKeyAsync(keyIdToRevoke, Arg.Any<CancellationToken>());
        await _mockPersistence.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeKeyAsync_WithNullKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        Func<Task> act = () => store.RevokeKeyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RevokeKeyAsync_WithEmptyKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        Func<Task> act = () => store.RevokeKeyAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetKeyById Tests

    [Fact]
    public async Task GetKeyByIdAsync_WithValidKeyId_ShouldReturnKey()
    {
        // Arrange
        const string keyId = "test-key-id";
        var testKey = new JwtSigningKey
        {
            Id = "test-id",
            KeyId = keyId,
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = false
        };

        _mockPersistence.GetKeyByIdAsync(keyId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((JwtSigningKey?)testKey));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        var result = await store.GetKeyByIdAsync(keyId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SymmetricSecurityKey>();
    }

    [Fact]
    public async Task GetKeyByIdAsync_WithInvalidKeyId_ShouldReturnNull()
    {
        // Arrange
        _mockPersistence.GetKeyByIdAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((JwtSigningKey?)null));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        var result = await store.GetKeyByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetKeyByIdAsync_WithNullKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        Func<Task> act = () => store.GetKeyByIdAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetSigningCredentials Tests

    [Fact]
    public async Task GetSigningCredentialsAsync_WithValidKey_ShouldReturnCredentials()
    {
        // Arrange
        var testKey = new JwtSigningKey
        {
            Id = "test-id",
            KeyId = "key-1",
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = true
        };

        _mockPersistence.GetCurrentSigningKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((JwtSigningKey?)testKey));

        var store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Act
        var credentials = await store.GetSigningCredentialsAsync();

        // Assert
        credentials.Should().NotBeNull();
        credentials.Key.Should().NotBeNull();
        credentials.Algorithm.Should().Be("HS256");
    }

    #endregion
}
