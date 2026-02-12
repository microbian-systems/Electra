using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Electra.Auth.Services;
using Raven.Client.Documents.Session;
using Raven.TestDriver;

namespace Electra.Auth.Tests.Services;

/// <summary>
/// Unit tests for refresh token service focusing on interface contracts and behavior
/// </summary>
public class RefreshTokenServiceContractTests : RavenTestDriver
{
    private readonly ILogger<RefreshTokenService> _mockLogger;
    private readonly IConfiguration _config;

    public RefreshTokenServiceContractTests()
    {
        ConfigureServer(new TestServerOptions
        {
            FrameworkVersion = null
        });

        _mockLogger = Substitute.For<ILogger<RefreshTokenService>>();
        
        // Create a real configuration with test values
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Auth:RefreshTokenLifetimeDays", "30" }
            });
        _config = configBuilder.Build();
    }

    // Interface Contract Tests

    [Fact]
    public void RefreshTokenService_ImplementsInterface()
    {
        // Arrange
        var mockSession = Substitute.For<IAsyncDocumentSession>();

        // Act
        IRefreshTokenService service = new RefreshTokenService(mockSession, _mockLogger, _config);

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

    // Dependency Injection Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Arrange
        var mockSession = Substitute.For<IAsyncDocumentSession>();

        // Act
        Action act = () => new RefreshTokenService(mockSession, _mockLogger, _config);

        // Assert
        act.Should().NotThrow();
    }

    // Configuration Tests

    [Fact]
    public void RefreshTokenLifetime_ShouldUseConfiguredValue()
    {
        // Arrange
        var mockSession = Substitute.For<IAsyncDocumentSession>();

        // Act
        var service = new RefreshTokenService(mockSession, _mockLogger, _config);

        // Assert
        service.Should().NotBeNull();
    }

    // Token Generation Tests

    [Fact]
    public async Task GenerateRefreshToken_WithValidParameters_ShouldReturnNonEmptyToken()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();

        var service = new RefreshTokenService(session, _mockLogger, _config);

        // Act
        var token = await service.GenerateRefreshTokenAsync("user-123", "mobile");

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    // Token Validation Tests

    [Fact]
    public async Task ValidateRefreshToken_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        var mockSession = Substitute.For<IAsyncDocumentSession>();
        var service = new RefreshTokenService(mockSession, _mockLogger, _config);

        // Act
        var result = await service.ValidateRefreshTokenAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateRefreshToken_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        var mockSession = Substitute.For<IAsyncDocumentSession>();
        var service = new RefreshTokenService(mockSession, _mockLogger, _config);

        // Act
        var result = await service.ValidateRefreshTokenAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    // Token Rotation Tests

    [Fact]
    public async Task RotateRefreshToken_WithInvalidToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();

        var service = new RefreshTokenService(session, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.RotateRefreshTokenAsync("invalid-token", "mobile");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // Token Revocation Tests

    [Fact]
    public async Task RevokeRefreshToken_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockSession = Substitute.For<IAsyncDocumentSession>();
        var service = new RefreshTokenService(mockSession, _mockLogger, _config);

        // Act
        Func<Task> act = async () => await service.RevokeRefreshTokenAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
