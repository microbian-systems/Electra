using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;

namespace Electra.Auth.Secrets;

public interface ISecretManager
{
    public string[]? CreateFragments(byte[]? secret, int nbFragments);

    public byte[]? ComputeFragments(string[] fragments);
}

public class ShamirsSecretManager : SecretManager
{
    public override string[]? CreateFragments(string? secret, int numFragments = 3)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        return CreateFragments(bytes, numFragments);
    }

    public override string[]? CreateFragments(byte[]? secret, int nbFragments = 3)
    {
            ArgumentNullException.ThrowIfNull(secret);

            if (secret == null || secret.Length == 0)
            {
                return null;
            }
            if (nbFragments < 3)
            {
                throw new ArgumentException("The number of fragments should at least be 3.", nameof(nbFragments));
            }
            var min = (int)Math.Ceiling((double)nbFragments * 2 / 3);
            var gcd = new SecretSharingDotNet.Math.ExtendedEuclideanAlgorithm<BigInteger>();
            var sss = new SecretSharingDotNet.Cryptography.ShamirsSecretSharing<BigInteger>(gcd);
            var shares = sss.MakeShares(min, nbFragments, secret);
            var fragments = new string[shares.Count];
            for (int i = 0; i < fragments.Length; ++i)
            {
                fragments[i] = shares[i].ToString();
            }
            return fragments;
    }

    public override byte[]? ComputeFragments(string[] fragments)
    {
         ArgumentNullException.ThrowIfNull(fragments);

         var gcd = new SecretSharingDotNet.Math.ExtendedEuclideanAlgorithm<BigInteger>();
         var combine = new SecretSharingDotNet.Cryptography.ShamirsSecretSharing<BigInteger>(gcd);
         var shares = string.Join(Environment.NewLine, fragments);
         var secret = combine.Reconstruction(shares);
         return secret.ToByteArray();
    }
}

public abstract class SecretManager : ISecretManager
{
    public abstract string[]? CreateFragments(string? secret, int numFragments = 3);
    public abstract string[]? CreateFragments(byte[]? secret, int numFragments = 3);
    public abstract byte[]? ComputeFragments(string[] fragments);
}