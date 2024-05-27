namespace Electra.Core.Algorithms;

public interface ISecretManager
{
    string[]? CreateFragments(string? secret, ushort numFragments = 3);
    string[]? CreateFragments(byte[]? secret, ushort nbFragments);
    byte[]? ComputeFragments(string[] fragments);
}