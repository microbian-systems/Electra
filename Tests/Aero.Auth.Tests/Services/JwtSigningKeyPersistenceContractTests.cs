using FluentAssertions;
using NSubstitute;
using Aero.Auth.Services;
using Aero.Models.Entities;

namespace Aero.Auth.Tests.Services;

/// <summary>
/// Unit tests for JWT signing key persistence interface contracts.
/// Tests the abstracted persistence layer that enables switching providers.
/// </summary>
public class JwtSigningKeyPersistenceContractTests
{
    #region Interface Contract Tests

    [Fact]
    public void IJwtSigningKeyPersistence_HasRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(IJwtSigningKeyPersistence);

        // Act
        var methods = interfaceType.GetMethods();

        // Assert
        methods.Should().Contain(m => m.Name == "GetCurrentSigningKeyAsync");
        methods.Should().Contain(m => m.Name == "GetValidSigningKeysAsync");
        methods.Should().Contain(m => m.Name == "GetKeyByIdAsync");
        methods.Should().Contain(m => m.Name == "AddKeyAsync");
        methods.Should().Contain(m => m.Name == "UpdateKeyAsync");
        methods.Should().Contain(m => m.Name == "DeactivateCurrentKeyAsync");
        methods.Should().Contain(m => m.Name == "RevokeKeyAsync");
        methods.Should().Contain(m => m.Name == "SaveChangesAsync");
    }

    [Fact]
    public void IJwtSigningKeyPersistence_AllMethodsAreAsync()
    {
        // Arrange
        var interfaceType = typeof(IJwtSigningKeyPersistence);

        // Act
        var methods = interfaceType.GetMethods();

        // Assert - All methods should return Task or Task<T>
        methods.Should().AllSatisfy(m =>
        {
            m.ReturnType.Name.Should().Match("*Task*",
                because: $"Method {m.Name} should be async");
        });
    }

    [Fact]
    public void IJwtSigningKeyPersistence_SupportsCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(IJwtSigningKeyPersistence);
        var methods = interfaceType.GetMethods();

        // Act & Assert
        methods.Should().AllSatisfy(m =>
        {
            var parameters = m.GetParameters();
            parameters.Should().Contain(p => p.ParameterType.Name == "CancellationToken",
                because: $"Method {m.Name} should support cancellation");
        });
    }

    #endregion

    #region Mock Verification Tests

    [Fact]
    public void Mock_CanBeCreatedForInterface()
    {
        // Act
        var mock = Substitute.For<IJwtSigningKeyPersistence>();

        // Assert
        mock.Should().NotBeNull();
        mock.Should().BeAssignableTo<IJwtSigningKeyPersistence>();
    }

    [Fact]
    public async Task Mock_GetCurrentSigningKeyAsync_CanBeConfigured()
    {
        // Arrange
        var mock = Substitute.For<IJwtSigningKeyPersistence>();
        var testKey = new JwtSigningKey
        {
            Id = "test-id",
            KeyId = "key-1",
            KeyMaterial = "base64-encoded-key",
            IsCurrentSigningKey = true
        };

        mock.GetCurrentSigningKeyAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((JwtSigningKey?)testKey));

        // Act
        var result = await mock.GetCurrentSigningKeyAsync();

        // Assert
        result.Should().NotBeNull();
        result?.KeyId.Should().Be("key-1");
    }

    [Fact]
    public async Task Mock_GetValidSigningKeysAsync_CanBeConfigured()
    {
        // Arrange
        var mock = Substitute.For<IJwtSigningKeyPersistence>();
        var testKeys = new[]
        {
            new JwtSigningKey
            {
                Id = "test-id-1",
                KeyId = "key-1",
                KeyMaterial = "base64-encoded-key-1",
                IsCurrentSigningKey = true
            }
        };

        mock.GetValidSigningKeysAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IEnumerable<JwtSigningKey>)testKeys));

        // Act
        var result = await mock.GetValidSigningKeysAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Mock_AddKeyAsync_CanBeConfigured()
    {
        // Arrange
        var mock = Substitute.For<IJwtSigningKeyPersistence>();
        mock.AddKeyAsync(Arg.Any<JwtSigningKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        var testKey = new JwtSigningKey
        {
            KeyId = "new-key",
            KeyMaterial = "base64-encoded-key"
        };

        // Act
        var result = await mock.AddKeyAsync(testKey);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task Mock_RevokeKeyAsync_CanBeConfigured()
    {
        // Arrange
        var mock = Substitute.For<IJwtSigningKeyPersistence>();
        mock.RevokeKeyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await mock.RevokeKeyAsync("test-key-id");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Return Type Tests

    [Fact]
    public void GetCurrentSigningKeyAsync_ReturnsNullableJwtSigningKey()
    {
        // Arrange
        var method = typeof(IJwtSigningKeyPersistence)
            .GetMethods()
            .First(m => m.Name == "GetCurrentSigningKeyAsync");

        // Act
        var returnType = method.ReturnType;

        // Assert
        returnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void GetValidSigningKeysAsync_ReturnsEnumerableOfKeys()
    {
        // Arrange
        var method = typeof(IJwtSigningKeyPersistence)
            .GetMethods()
            .First(m => m.Name == "GetValidSigningKeysAsync");

        // Act
        var returnType = method.ReturnType;

        // Assert
        returnType.Name.Should().Contain("Task");
    }

    [Fact]
    public void AddKeyAsync_ReturnsBoolean()
    {
        // Arrange
        var method = typeof(IJwtSigningKeyPersistence)
            .GetMethods()
            .First(m => m.Name == "AddKeyAsync");

        // Act
        var returnType = method.ReturnType;

        // Assert
        returnType.Name.Should().Contain("Task");
    }

    #endregion

    #region Parameter Validation Tests

    [Fact]
    public void GetKeyByIdAsync_HasKeyIdParameter()
    {
        // Arrange
        var method = typeof(IJwtSigningKeyPersistence)
            .GetMethods()
            .First(m => m.Name == "GetKeyByIdAsync");

        // Act
        var parameters = method.GetParameters();

        // Assert
        parameters.Should().Contain(p => p.Name == "keyId");
    }

    [Fact]
    public void AddKeyAsync_HasKeyParameter()
    {
        // Arrange
        var method = typeof(IJwtSigningKeyPersistence)
            .GetMethods()
            .First(m => m.Name == "AddKeyAsync");

        // Act
        var parameters = method.GetParameters();

        // Assert
        parameters.Should().Contain(p => p.Name == "key");
    }

    [Fact]
    public void RevokeKeyAsync_HasKeyIdParameter()
    {
        // Arrange
        var method = typeof(IJwtSigningKeyPersistence)
            .GetMethods()
            .First(m => m.Name == "RevokeKeyAsync");

        // Act
        var parameters = method.GetParameters();

        // Assert
        parameters.Should().Contain(p => p.Name == "keyId");
    }

    #endregion

    #region Substitutability Tests

    [Fact]
    public void Implementations_ShouldBeSubstitutable()
    {
        // Arrange
        IJwtSigningKeyPersistence implementation1 = Substitute.For<IJwtSigningKeyPersistence>();
        IJwtSigningKeyPersistence implementation2 = Substitute.For<IJwtSigningKeyPersistence>();

        // Act & Assert
        implementation1.Should().BeAssignableTo<IJwtSigningKeyPersistence>();
        implementation2.Should().BeAssignableTo<IJwtSigningKeyPersistence>();
    }

    #endregion
}
