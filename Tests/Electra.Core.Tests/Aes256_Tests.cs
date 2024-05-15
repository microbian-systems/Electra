using System.Security.Cryptography;
using Electra.Core.Encryption;

namespace Electra.Core.Tests;

public class Aes256_Tests
{
    [Fact]
    public void Encrypt_String_Success()
    {
        var key = new byte[32]; // AES-256 requires a 32-byte key (256 bits)
        var iv = new byte[16];  // AES uses a 16-byte initialization vector (IV)

        // Initialize the key and IV with random bytes (for demonstration purposes)
        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(key);
        rng.GetBytes(iv);

        var encryption = new Aes256Encryptor(key, iv);
        var plainText = "Hello, world!";
        var encryptedText = encryption.EncryptString(plainText);

        encryptedText.Should().NotBeNullOrEmpty();
        encryptedText.Should().NotBeEquivalentTo(plainText);
    }

    [Fact]
    public void To_Base64_Should_Not_Be_Unencrypted_Success()
    {
        // Example usage:
        var key = new byte[32]; // AES-256 requires a 32-byte key (256 bits)
        var iv = new byte[16];  // AES uses a 16-byte initialization vector (IV)

        // Initialize the key and IV with random bytes (for demonstration purposes)
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
            rng.GetBytes(iv);
        }

        var encryption = new Aes256Encryptor(key, iv);
        var plainText = "Hello, world!";
        var encryptedText = encryption.EncryptString(plainText);
        var bytes = Convert.FromBase64String(encryptedText);
        var base64Decoded = Encoding.UTF8.GetString(bytes);

        encryptedText.Should().NotBeNullOrEmpty();
        encryptedText.Should().NotBeEquivalentTo(plainText);
        base64Decoded.Should().NotBeEquivalentTo(plainText);
    }

    [Fact]
    public void Decrypt_String_Success()
    {
        // Example usage:
        var key = new byte[32]; // AES-256 requires a 32-byte key (256 bits)
        var iv = new byte[16];  // AES uses a 16-byte initialization vector (IV)

        // Initialize the key and IV with random bytes (for demonstration purposes)
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
            rng.GetBytes(iv);
        }

        var encryption = new Aes256Encryptor(key, iv);
        var plainText = "Hello, world!";
        var encryptedText = encryption.EncryptString(plainText);
        var decryptedText = encryption.DecryptString(encryptedText);

        plainText.Should().NotBeNullOrEmpty(decryptedText);
        decryptedText.Should().BeEquivalentTo(plainText);
    }
}