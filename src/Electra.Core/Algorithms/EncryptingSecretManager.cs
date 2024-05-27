using Electra.Core.Encryption;

namespace Electra.Core.Algorithms;

public sealed class EncryptingSecretManager(
    ISecretManager manager,
    IEncryptor encryptor,
    ILogger<IEncryptingSecretManager> log) : IEncryptingSecretManager
{
    public string[]? CreateFragments(string? secret, ushort numFragments = 3)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        var frags = manager.CreateFragments(Encoding.UTF8.GetBytes(secret), numFragments);
        var encrypted = frags.Select(encryptor.EncryptString)
            .ToArray();

        return encrypted;
    }

    public string[]? CreateFragments(byte[]? secret, ushort nbFragments = 3)
    {
        log.LogInformation("encrypting fragments");

        var shards = manager.CreateFragments(secret, nbFragments);

        if (shards is null)
        {
            log.LogError("Failed to create fragments.");
            return [];
        }

        if (shards.Length == 0)
        {
            log.LogWarning("no secret fragments were created.");
            return [];
        }

        var encrypted = shards.Select(encryptor.EncryptString);

        return encrypted.ToArray();
    }

    public byte[] ComputeFragments(string[] fragments)
    {
        log.LogInformation("decrypting fragments");
        var decryptedFraments = fragments.Select(encryptor.DecryptString);
        var frags = manager.ComputeFragments(decryptedFraments.ToArray());

        return frags;
    }
}