using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Electra.Auth.Services;
using Electra.Models.Entities;
using Electra.Persistence.RavenDB;
using Raven.TestDriver;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Embedded;

namespace Electra.Auth.Tests.Services;

/// <summary>
/// Unit tests for RavenDB JWT signing key persistence implementation.
/// Tests the concrete RavenDB provider implementation.
/// </summary>
public class RavenDbJwtSigningKeyPersistenceTests : RavenTestDriver
{
    private readonly IRavenDbUnitOfWork _mockUow;
    private readonly ILogger<RavenDbJwtSigningKeyPersistence> _mockLogger;

    static RavenDbJwtSigningKeyPersistenceTests()
    {
        // Configure RavenDB TestDriver
        ConfigureServer(new TestServerOptions
        {
            FrameworkVersion = null, // Use default
            Licensing = new ServerOptions.LicensingOptions
            {
                ThrowOnInvalidOrMissingLicense = false
            },
            CommandLineArgs = new List<string>
            {
                "--RunInMemory=true"
            }
        });
    }

    public RavenDbJwtSigningKeyPersistenceTests()
    {
        _mockUow = Substitute.For<IRavenDbUnitOfWork>();
        _mockLogger = Substitute.For<ILogger<RavenDbJwtSigningKeyPersistence>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldNotThrow()
    {
        // Act
        Action act = () => new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullUow_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new RavenDbJwtSigningKeyPersistence(null!, _mockLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("uow");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new RavenDbJwtSigningKeyPersistence(_mockUow, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void RavenDbJwtSigningKeyPersistence_ImplementsInterface()
    {
        // Arrange & Act
        IJwtSigningKeyPersistence persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Assert
        persistence.Should().BeAssignableTo<IJwtSigningKeyPersistence>();
    }

    [Fact]
    public void RavenDbJwtSigningKeyPersistence_HasAllRequiredMethods()
    {
        // Arrange
        var implementationType = typeof(RavenDbJwtSigningKeyPersistence);
        var interfaceType = typeof(IJwtSigningKeyPersistence);

        // Act
        var interfaceMethods = interfaceType.GetMethods();
        var implementationMethods = implementationType.GetMethods();

        // Assert
        foreach (var method in interfaceMethods)
        {
            implementationMethods.Should().Contain(m =>
                m.Name == method.Name && m.ReturnType == method.ReturnType,
                because: $"Implementation should have method {method.Name}");
        }
    }

    #endregion

    #region AddKey Tests

    [Fact]
    public async Task AddKeyAsync_WithValidKey_ShouldReturnTrue()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var uow = new RavenDbUnitOfWork(session, Substitute.For<ILogger<RavenDbUnitOfWork>>(), Substitute.For<ILoggerFactory>());
        var persistence = new RavenDbJwtSigningKeyPersistence(uow, _mockLogger);
        
        var keyToAdd = new JwtSigningKey
        {
            KeyId = "new-key",
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = true
        };

        // Act
        var result = await persistence.AddKeyAsync(keyToAdd);
        await uow.SaveChangesAsync(); // Ensure it persists
        WaitForIndexing(store);

        // Assert
        result.Should().BeTrue();

        // Verify it was actually added
        using var verifySession = store.OpenAsyncSession();
        var savedKey = await verifySession.Query<JwtSigningKey>().FirstOrDefaultAsync(k => k.KeyId == "new-key");
        savedKey.Should().NotBeNull();
        savedKey!.IsCurrentSigningKey.Should().BeTrue();
    }

    [Fact]
    public async Task AddKeyAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Act
        Func<Task> act = () => persistence.AddKeyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateKey Tests

    [Fact]
    public async Task UpdateKeyAsync_WithValidKey_ShouldReturnTrue()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var uow = new RavenDbUnitOfWork(session, Substitute.For<ILogger<RavenDbUnitOfWork>>(), Substitute.For<ILoggerFactory>());
        var persistence = new RavenDbJwtSigningKeyPersistence(uow, _mockLogger);

        var existingKey = new JwtSigningKey
        {
            KeyId = "existing-key",
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = true
        };
        await persistence.AddKeyAsync(existingKey);
        await uow.SaveChangesAsync();
        WaitForIndexing(store);

        var keyToUpdate = new JwtSigningKey
        {
            Id = existingKey.Id, // Must match the ID assigned by RavenDB (or set manually)
            KeyId = "existing-key",
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = false
        };

        // Act
        // Re-create persistence/session to simulate fresh request? 
        // Or just use same session (UnitOfWork pattern usually implies same session for logic)
        // But UpdateKeyAsync uses GetSession() which calls _uow.Session.
        var result = await persistence.UpdateKeyAsync(keyToUpdate);
        await uow.SaveChangesAsync();
        WaitForIndexing(store);

        // Assert
        result.Should().BeTrue();
        
        using var verifySession = store.OpenAsyncSession();
        var updatedKey = await verifySession.LoadAsync<JwtSigningKey>(existingKey.Id);
        updatedKey.IsCurrentSigningKey.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateKeyAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Act
        Func<Task> act = () => persistence.UpdateKeyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region RevokeKey Tests

    [Fact]
    public async Task RevokeKeyAsync_WithValidKeyId_ShouldReturnTrue()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        var uow = new RavenDbUnitOfWork(session, Substitute.For<ILogger<RavenDbUnitOfWork>>(), Substitute.For<ILoggerFactory>());
        var persistence = new RavenDbJwtSigningKeyPersistence(uow, _mockLogger);

        var existingKey = new JwtSigningKey
        {
            KeyId = "valid-key-id",
            KeyMaterial = Convert.ToBase64String(new byte[32]),
            IsCurrentSigningKey = true
        };
        await persistence.AddKeyAsync(existingKey);
        await uow.SaveChangesAsync();
        WaitForIndexing(store);

        // Act
        var result = await persistence.RevokeKeyAsync("valid-key-id");
        await uow.SaveChangesAsync();
        WaitForIndexing(store);

        // Assert
        result.Should().BeTrue();

        using var verifySession = store.OpenAsyncSession();
        var revokedKey = await verifySession.LoadAsync<JwtSigningKey>(existingKey.Id);
        revokedKey.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeKeyAsync_WithNullKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Act
        Func<Task> act = () => persistence.RevokeKeyAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RevokeKeyAsync_WithEmptyKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Act
        Func<Task> act = () => persistence.RevokeKeyAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetKeyById Tests

    [Fact]
    public async Task GetKeyByIdAsync_WithNullKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Act
        Func<Task> act = () => persistence.GetKeyByIdAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetKeyByIdAsync_WithEmptyKeyId_ShouldThrowArgumentException()
    {
        // Arrange
        var persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Act
        Func<Task> act = () => persistence.GetKeyByIdAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region SaveChanges Tests

    [Fact]
    public async Task SaveChangesAsync_ShouldCallUowSaveChanges()
    {
        // Arrange
        _mockUow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(1));

        var persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Act
        var result = await persistence.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();
        await _mockUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Input Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Methods_WithInvalidStringParameters_ShouldValidate(string? invalidValue)
    {
        // Arrange
        var persistence = new RavenDbJwtSigningKeyPersistence(_mockUow, _mockLogger);

        // Act & Assert
        if (invalidValue == null)
        {
            await persistence.Invoking(p => p.GetKeyByIdAsync(invalidValue!))
                .Should().ThrowAsync<ArgumentException>();
        }
        else
        {
            await persistence.Invoking(p => p.GetKeyByIdAsync(invalidValue))
                .Should().ThrowAsync<ArgumentException>();
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task AddKeyAsync_ShouldReturnFalseOnException()
    {
        // Arrange
        using var store = GetDocumentStore();
        using var session = store.OpenAsyncSession();
        // Force disposal of session to cause exception? 
        // Or mock internal behavior? 
        // The original test mocked UOW to verify error handling.
        // We can use the mock UOW here since we want to simulate exception from underlying storage which is hard with real DB unless we break connection.
        
        var mockUow = Substitute.For<IRavenDbUnitOfWork>();
        var mockSession = Substitute.For<IAsyncDocumentSession>();
        mockUow.Session.Returns(mockSession);
        mockSession.StoreAsync(Arg.Any<JwtSigningKey>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Database error")));
            
        var persistence = new RavenDbJwtSigningKeyPersistence(mockUow, _mockLogger);
        
        var keyToAdd = new JwtSigningKey
        {
            KeyId = "key",
            KeyMaterial = Convert.ToBase64String(new byte[32])
        };

        // Act
        var result = await persistence.AddKeyAsync(keyToAdd);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}