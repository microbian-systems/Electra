using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Electra.Auth.Services;

namespace Electra.Auth.Tests.Services;

/// <summary>
/// Simplified unit tests for JWT token service
/// Focuses on configuration, error handling, and token lifetime
/// </summary>
public class JwtTokenServiceSimplifiedTests
{
    private readonly IJwtSigningKeyStore _mockKeyStore;
    private readonly ILogger<JwtTokenService> _mockLogger;
    private readonly IConfiguration _mockConfig;

    public JwtTokenServiceSimplifiedTests()
    {
        _mockKeyStore = Substitute.For<IJwtSigningKeyStore>();
        _mockLogger = Substitute.For<ILogger<JwtTokenService>>();
        _mockConfig = Substitute.For<IConfiguration>();
    }

    #region Configuration Tests

    [Fact]
    public void Constructor_WithValidConfig_ShouldSetAccessTokenLifetime()
    {
        // Arrange
        _mockConfig["Auth:AccessTokenLifetimeSeconds"].Returns("600");

        // Act
        var service = new JwtTokenService(_mockKeyStore, _mockLogger, _mockConfig);

        // Assert
        service.AccessTokenLifetime.Should().Be(600);
    }

    [Fact]
    public void Constructor_WithoutAccessTokenConfig_ShouldUseDefault()
    {
        // Arrange
        _mockConfig["Auth:AccessTokenLifetimeSeconds"].Returns((string?)null);

        // Act
        var service = new JwtTokenService(_mockKeyStore, _mockLogger, _mockConfig);

        // Assert
        service.AccessTokenLifetime.Should().Be(300);
    }

    [Fact]
    public void Constructor_WithMultipleInstances_ShouldEachHaveOwnConfig()
    {
        // Arrange
        var config1 = Substitute.For<IConfiguration>();
        var config2 = Substitute.For<IConfiguration>();
        
        config1["Auth:AccessTokenLifetimeSeconds"].Returns("300");
        config2["Auth:AccessTokenLifetimeSeconds"].Returns("600");

        // Act
        var service1 = new JwtTokenService(_mockKeyStore, _mockLogger, config1);
        var service2 = new JwtTokenService(_mockKeyStore, _mockLogger, config2);

        // Assert
        service1.AccessTokenLifetime.Should().Be(300);
        service2.AccessTokenLifetime.Should().Be(600);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GenerateAccessToken_WithNullKeyStore_ShouldThrowNullReferenceException()
    {
        // Arrange
        var service = new JwtTokenService(null!, _mockLogger, _mockConfig);

        // Act
        Func<Task> act = async () => await service.GenerateAccessTokenAsync("user-123", "test@example.com");

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task GenerateAccessToken_WithKeyStoreThrowing_ShouldPropagateException()
    {
        // Arrange
        var service = new JwtTokenService(_mockKeyStore, _mockLogger, _mockConfig);
        _mockKeyStore.GetSigningCredentialsAsync(Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(Task.FromException<Microsoft.IdentityModel.Tokens.SigningCredentials>(
                new InvalidOperationException("No signing key")));

        // Act
        Func<Task> act = async () => await service.GenerateAccessTokenAsync("user-123", "test@example.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region Dependency Injection Tests

    [Fact]
    public void ServiceImplementsInterface_ShouldBeRegistrable()
    {
        // Arrange & Act
        IJwtTokenService service = new JwtTokenService(_mockKeyStore, _mockLogger, _mockConfig);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IJwtTokenService>();
    }

    #endregion

    #region Configuration Value Tests

    [Theory]
    [InlineData("100")]
    [InlineData("300")]
    [InlineData("600")]
    [InlineData("900")]
    public void AccessTokenLifetime_WithVariousConfigs_ShouldReturnCorrectValue(string configValue)
    {
        // Arrange
        _mockConfig["Auth:AccessTokenLifetimeSeconds"].Returns(configValue);
        var service = new JwtTokenService(_mockKeyStore, _mockLogger, _mockConfig);

        // Act
        var lifetime = service.AccessTokenLifetime;

        // Assert
        lifetime.Should().Be(int.Parse(configValue));
    }

    #endregion
}
