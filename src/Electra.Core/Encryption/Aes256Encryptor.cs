using System.Security.Cryptography;

namespace Electra.Core.Encryption;

public interface IEncryptor
{
    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="text">the string to be encrypted</param>
    /// <returns></returns>
    string EncryptString(string text);

    /// <summary>
    /// Decrypts an already encrypted string
    /// </summary>
    /// <param name="encrypted">the string to be decrypted</param>
    /// <returns></returns>
    string DecryptString(string encrypted);
}

/// <summary>
/// The Aes256Encryptor class provides methods for encrypting and decrypting strings using the
/// AES-256 encryption algorithm. It requires a 32-byte key and a 16-byte initialization vector
/// (IV) for the encryption and decryption processes. The EncryptString method takes a plain text
/// string, encrypts it using AES-256, and returns the encrypted string in base64 format.
/// The DecryptString method takes a base64 encoded encrypted string, decrypts it using AES-256,
/// and returns the original plain text string.
/// </summary>
/// <param name="key">AES-256 requires a 32-byte key (256 bits)</param>
/// <param name="iv">AES uses a 16-byte initialization vector (IV)</param>
public class Aes256Encryptor(byte[] key, byte[] iv) : IEncryptor
{
    /// <summary>
    /// Encrypts a string
    /// </summary>
    /// <param name="text">the string to be encrypted</param>
    /// <returns></returns>
    public string EncryptString(string text)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;

        // Create an encryptor to perform the stream transform.
        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        var inputBytes = Encoding.UTF8.GetBytes(text);
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

        // Encrypt the input data to the CryptoStream.
        csEncrypt.Write(inputBytes, 0, inputBytes.Length);
        csEncrypt.FlushFinalBlock();

        // Convert the encrypted data from a byte array to a base64-encoded string.
        var encryptedBytes = msEncrypt.ToArray();
        var encryptedBase64 = Convert.ToBase64String(encryptedBytes);

        return encryptedBase64;
    }

    /// <summary>
    /// Decrypts an already encrypted string
    /// </summary>
    /// <param name="encrypted">the string to be decrypted</param>
    /// <returns></returns>
    public string DecryptString(string encrypted)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;

        // Create a decryptor to perform the stream transform.
        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        // Convert the base64-encoded string back to bytes.
        var encryptedBytes = Convert.FromBase64String(encrypted);

        // Create a memory stream to hold the decrypted bytes.
        using var msDecrypt = new MemoryStream(encryptedBytes);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

        // Read the decrypted bytes from the CryptoStream and convert them to a string.
        using var srDecrypt = new StreamReader(csDecrypt);
        var secret = srDecrypt.ReadToEnd();

        return secret;
    }
}