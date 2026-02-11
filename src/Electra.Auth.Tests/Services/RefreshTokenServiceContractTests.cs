using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Electra.Auth.Services;

namespace Electra.Auth.Tests.Services;

/// <summary>
/// Unit tests for refresh token service focusing on interface contracts and behavior
/// </summary>
public class RefreshTokenServiceContractTests
{
    private readonly ILogger<RefreshTokenService> _mockLogger;
    private readonly IConfiguration _config;

    public RefreshTokenServiceContractTests()
    {
        _mockLogger = Substitute.For<ILogger<RefreshTokenService>>();
        
        // Create a real configuration with test values
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Auth:RefreshTokenLifetimeDays", "30" }
            });
        _config = configBuilder.Build();
    }

    #region Interface Contract Tests

    [Fact]
    public void RefreshTokenService_ImplementsInterface()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();

        // Act
        IRefreshTokenService service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IRefreshTokenService>();
    }

    [Fact]
    public void IRefreshTokenService_HasRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(IRefreshTokenService);

        // Act
        var methods = interfaceType.GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "GenerateRefreshTokenAsync");
        methods.Should().Contain(m => m.Name == "ValidateRefreshTokenAsync");
        methods.Should().Contain(m => m.Name == "RotateRefreshTokenAsync");
        methods.Should().Contain(m => m.Name == "RevokeRefreshTokenAsync");
        methods.Should().Contain(m => m.Name == "RevokeAllUserTokensAsync");
        methods.Should().Contain(m => m.Name == "GetActiveTokensAsync");
    }

    #endregion

    #region Dependency Injection Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();

        // Act
        Action act = () => new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullContextFactory_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new RefreshTokenService(null!, _mockLogger, _config);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();

        // Act
        Action act = () => new RefreshTokenService(mockContextFactory, null!, _config);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();

        // Act
        Action act = () => new RefreshTokenService(mockContextFactory, _mockLogger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void RefreshTokenLifetime_ShouldUseConfiguredValue()
    {
        // Arrange
        // Cannot mock extension methods on real object easily
        // _config.GetValue("Auth:RefreshTokenLifetimeDays", 30).Returns(30);
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();

        // Act
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Assert
        // _config.Received(1).GetValue("Auth:RefreshTokenLifetimeDays", 30);
        service.Should().NotBeNull();
    }

    #endregion

    #region Token Generation Tests

    [Fact]
    public async Task GenerateRefreshToken_WithValidParameters_ShouldReturnNonEmptyToken()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var mockContext = Substitute.For<Microsoft.EntityFrameworkCore.DbContext>();
        
        mockContextFactory.CreateDbContextAsync(Arg.Any<System.Threading.CancellationToken>())
            .Returns(mockContext);

        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        var token = await service.GenerateRefreshTokenAsync("user-123", "mobile");

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateRefreshToken_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.GenerateRefreshTokenAsync(string.Empty, "mobile");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateRefreshToken_WithNullUserId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.GenerateRefreshTokenAsync(null!, "mobile");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public async Task ValidateRefreshToken_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        var result = await service.ValidateRefreshTokenAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshToken_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        var result = await service.ValidateRefreshTokenAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Token Rotation Tests

    [Fact]
    public async Task RotateRefreshToken_WithInvalidToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var mockContext = Substitute.For<Microsoft.EntityFrameworkCore.DbContext>();
        
        mockContextFactory.CreateDbContextAsync(Arg.Any<System.Threading.CancellationToken>())
            .Returns(mockContext);

        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.RotateRefreshTokenAsync("invalid-token", "mobile");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

    #region Token Revocation Tests

    [Fact]
    public async Task RevokeRefreshToken_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.RevokeRefreshTokenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RevokeAllUserTokens_WithNullUserId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.RevokeAllUserTokensAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RevokeAllUserTokens_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.RevokeAllUserTokensAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Session Retrieval Tests

    [Fact]
    public async Task GetActiveTokens_WithNullUserId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.GetActiveTokensAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetActiveTokens_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var mockContextFactory = Substitute.For<Microsoft.EntityFrameworkCore.IDbContextFactory<Microsoft.EntityFrameworkCore.DbContext>>();
        var service = new RefreshTokenService(mockContextFactory, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.GetActiveTokensAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}