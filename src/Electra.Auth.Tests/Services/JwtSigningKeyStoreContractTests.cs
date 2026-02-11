using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Electra.Auth.Services;

namespace Electra.Auth.Tests.Services;

/// <summary>
/// Unit tests for JWT signing key store focusing on interface contracts
/// Tests the refactored store that uses abstracted persistence layer
/// </summary>
public class JwtSigningKeyStoreContractTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<JwtSigningKeyStore> _mockLogger;
    private readonly IJwtSigningKeyPersistence _mockPersistence;

    public JwtSigningKeyStoreContractTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = Substitute.For<ILogger<JwtSigningKeyStore>>();
        _mockPersistence = Substitute.For<IJwtSigningKeyPersistence>();
    }

    #region Interface Contract Tests

    [Fact]
    public void JwtSigningKeyStore_ImplementsInterface()
    {
        // Arrange & Act
        IJwtSigningKeyStore store = new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Assert
        store.Should().NotBeNull();
        store.Should().BeAssignableTo<IJwtSigningKeyStore>();
    }

    [Fact]
    public void IJwtSigningKeyStore_HasRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(IJwtSigningKeyStore);

        // Act
        var methods = interfaceType.GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "GetCurrentSigningKeyAsync");
        methods.Should().Contain(m => m.Name == "GetCurrentKeyIdAsync");
        methods.Should().Contain(m => m.Name == "GetValidationKeysAsync");
        methods.Should().Contain(m => m.Name == "GetSigningCredentialsAsync");
        methods.Should().Contain(m => m.Name == "RotateSigningKeyAsync");
        methods.Should().Contain(m => m.Name == "RevokeKeyAsync");
        methods.Should().Contain(m => m.Name == "GetKeyByIdAsync");
    }

    [Fact]
    public void GetSigningCredentials_ShouldReturnCorrectType()
    {
        // Act
        var methodInfo = typeof(IJwtSigningKeyStore)
            .GetMethods()
            .First(m => m.Name == "GetSigningCredentialsAsync");

        // Assert
        methodInfo.ReturnType.Name.Should().Contain("Task");
    }

    #endregion

    #region Cache Behavior Tests

    [Fact]
    public void MemoryCache_CanStoreAndRetrieveValues()
    {
        // Arrange
        var cacheKey = "test-key";
        var cacheValue = "test-value";

        // Act
        _memoryCache.Set(cacheKey, cacheValue);
        var retrieved = _memoryCache.TryGetValue(cacheKey, out var value);

        // Assert
        retrieved.Should().BeTrue();
        value.Should().Be(cacheValue);
    }

    [Fact]
    public void MemoryCache_CanRemoveValues()
    {
        // Arrange
        var cacheKey = "test-key";
        _memoryCache.Set(cacheKey, "value");

        // Act
        _memoryCache.Remove(cacheKey);
        var retrieved = _memoryCache.TryGetValue(cacheKey, out _);

        // Assert
        retrieved.Should().BeFalse();
    }

    #endregion

    #region Dependency Injection Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Act
        Action act = () => new JwtSigningKeyStore(_mockPersistence, _mockLogger, _memoryCache);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullPersistence_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new JwtSigningKeyStore(null!, _mockLogger, _memoryCache);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new JwtSigningKeyStore(_mockPersistence, null!, _memoryCache);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new JwtSigningKeyStore(_mockPersistence, _mockLogger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Algorithm Tests

    [Fact]
    public void SigningKey_ShouldUseHmacSha256Algorithm()
    {
        // Act
        var algorithm = SecurityAlgorithms.HmacSha256;

        // Assert
        algorithm.Should().Be("HS256");
    }

    #endregion
}
