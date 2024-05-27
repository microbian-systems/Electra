using Electra.Core.Algorithms;
using Electra.Core.Encryption;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Electra.Core.Tests;

public class EncryptingSecretManagerTests
{
    private const string secret = "beer | 麦酒 (ばくしゅ | пиво | ビール | 酒 | 啤酒";

    private static ISecretManager CreateMockSecretManager() => new ShamirsSecretManager();

    private static ILogger<IEncryptingSecretManager> CreateMockLogger()
        =>A.Fake<ILogger<IEncryptingSecretManager>>();

    private static Aes256Encryptor CreateEncryptor()
    {
        var key = new byte[32]; // AES-256 requires a 32-byte key (256 bits)
        var iv = new byte[16];  // AES uses a 16-byte initialization vector (IV)

        // Initialize the key and IV with random bytes (for demonstration purposes)
        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(key);
        rng.GetBytes(iv);
        return new Aes256Encryptor(key, iv);
    }

    private EncryptingSecretManager CreateEncryptingSecretManager(ISecretManager manager, Aes256Encryptor encryptor, ILogger<IEncryptingSecretManager> log)
        => new EncryptingSecretManager(manager, encryptor, log);

    private ISecretManager CreateEncryptingSecretManager(out Aes256Encryptor encryptor,
        out EncryptingSecretManager encryptingManager)
    {
        var secretManager = CreateMockSecretManager();
        encryptor = CreateEncryptor();
        var logger = CreateMockLogger();
        encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor, logger);
        return secretManager;
    }
    
    [Fact]
    public void CreateFragments_WithStringInput_EncryptsFragments()
    {
        // Arrange
        var secretManager =
            CreateEncryptingSecretManager(out var encryptor, out var encryptingManager);
        //var secretBytes = Encoding.UTF8.GetBytes(secret);
        //var fragments = secretManager.CreateFragments(secretBytes, 3);

        // Act
        var fragments = encryptingManager.CreateFragments(secret, 3);

        // Assert
        Assert.NotNull(fragments);
        Assert.Equal(3, fragments.Length);

        var result = encryptingManager.ComputeFragments(fragments);
        var decrypted = encryptingManager.Deconstruct(result);

        decrypted.Should().BeEquivalentTo(secret);
    }
    

    [Fact]
    public void CreateFragments_WithByteArrayInput_EncryptsFragments()
    {
        // Arrange
        var secretManager = CreateMockSecretManager();
        var encryptor = CreateEncryptor();
        var logger = CreateMockLogger();
        var encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor, logger);
        var bytes = Encoding.UTF8.GetBytes(secret);

        // Act
        var result = encryptingManager.CreateFragments(bytes, 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void ComputeFragments_DecryptsFragments()
    {
        // Arrange
        var secretManager = CreateMockSecretManager();
        var encryptor = CreateEncryptor();
        var logger = CreateMockLogger();
        var encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor, logger);
        var encryptedFragments = encryptingManager.CreateFragments(secret, 3);
        var secretBytes = Encoding.UTF8.GetBytes(secret);


        // Act
        var result = encryptingManager.ComputeFragments(encryptedFragments);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secretBytes, result);
    }

    [Fact]
    public void CreateFragments_WithNullString_ThrowsArgumentException()
    {
        // Arrange
        var secretManager = A.Fake<ISecretManager>();
        var encryptor = A.Fake<IEncryptor>();
        var logger = A.Fake<ILogger<EncryptingSecretManager>>();
        var encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor as Aes256Encryptor, logger);

        // Act & Assert
        // Act & Assert
        Action act = () => encryptingManager.CreateFragments((string)null, 3);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'secret')");
    }

    // [Fact]
    // public void CreateFragments_WithNullByteArray_ReturnsEmptyArray()
    // {
    //     // Arrange
    //     var secretManager = CreateMockSecretManager();
    //     var encryptor = CreateEncryptor();
    //     var logger = CreateMockLogger();
    //     var encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor, logger);
    //
    //     // Act
    //     Assert.Throws<Exception>(() =>
    //         encryptingManager.CreateFragments((byte[])null, 3));
    //
    //     Assert.Throws<Exception>(() =>
    //         encryptingManager.CreateFragments(Array.Empty<byte>(), 3));
    //
    // }

    // [Fact]
    // public void CreateFragments_WithNullString_Exception_ThrowsArgumentNullException()
    // {
    //     // Arrange
    //     var secretManager = A.Fake<ISecretManager>();
    //     var encryptor = A.Fake<IEncryptor>();
    //     var logger = A.Fake<ILogger<EncryptingSecretManager>>();
    //     var encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor as Aes256Encryptor, logger);
    //     var encrypting = A.Fake<IEncryptingSecretManager>();
    //
    //     // Act & Assert
    //     Action act = () => encrypting.CreateFragments((string)null, 3);
    //     act.Should().Throw<ArgumentNullException>()
    //         .WithMessage("Value cannot be null. (Parameter 'secret')");
    // }

    [Fact]
    public void CreateFragments_WithNullByteArray_ThrowsArgumentException()
    {
        // Arrange
        var secretManager = CreateMockSecretManager();
        var encryptor = CreateEncryptor();
        var logger = CreateMockLogger();
        var encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor, logger);

        // Act & Assert
        Action act = () => encryptingManager.CreateFragments((byte[])null, 3);
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'secret')");
    }

    [Fact]
    public void CreateFragments_WithEmptyByteArray_ThrowsArgumentException()
    {
        // Arrange
        var secretManager = CreateMockSecretManager();
        var encryptor = CreateEncryptor();
        var logger = CreateMockLogger();
        var encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor, logger);

        // Act & Assert
        var results = encryptingManager.CreateFragments(Array.Empty<byte>(), 3);
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public void CreateFragments_WithZeroLengthFragments_ReturnsEmptyArray()
    {
        // Arrange
        var secretManager = CreateMockSecretManager();
        var encryptor = CreateEncryptor();
        var logger = CreateMockLogger();
        var encryptingManager = CreateEncryptingSecretManager(secretManager, encryptor, logger);
        var secret = Encoding.UTF8.GetBytes("");

        // Act
        var result = encryptingManager.CreateFragments(secret, 3);

        // Assert
        result.Should().BeEmpty();
    }
}