namespace Aero.Core.Algorithms;

public abstract class SecretManager : ISecretManager
{
    public virtual string[]? CreateFragments(string? secret, ushort numFragments = 3)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        return CreateFragments(Encoding.UTF8.GetBytes(secret), numFragments);
    }

    public abstract string[]? CreateFragments(byte[]? secret, ushort numFragments = 3);
    public abstract byte[]? ComputeFragments(string[] fragments);
}