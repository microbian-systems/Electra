using System.Numerics;
using SecretSharingDotNet.Cryptography.ShamirsSecretSharing;
using SecretSharingDotNet.Math;

namespace Aero.Core.Algorithms;

public class ShamirsSecretManager : SecretManager
{
    public override string[]? CreateFragments(string? secret, ushort numFragments = 3)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        return CreateFragments(bytes, numFragments);
    }

    public override string[]? CreateFragments(byte[]? secret, ushort nbFragments = 3)
    {
        ArgumentNullException.ThrowIfNull(secret);

        if (secret.Length == 0)
            return null;

        if (nbFragments < 3)
        {
            throw new ArgumentException("The number of fragments should at least be 3.", nameof(nbFragments));
        }
        var min = (ushort)Math.Ceiling((double)nbFragments * 2 / 3);
        var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();
        //// Create Shamir's Secret Sharing instance with BigInteger
        var splitter = new SecretSplitter<BigInteger>();
        // var sss = new 
        var shares = splitter.MakeShares(3, 7, 127);
        var combiner = new SecretReconstructor<BigInteger>(gcd);
        var subSet1 = shares.Where(p => p.X.IsEven).ToList();
        var recoveredSecret1 = combiner.Reconstruction(subSet1.ToArray());
        var subSet2 = shares.Where(p => !p.X.IsEven).ToList();
        var recoveredSecret2 = combiner.Reconstruction(subSet2.ToArray());
        
        // todo - verify update to .net 10 didn't break teh shamirs algo
        //var shares = sss.MakeShares(min, nbFragments, secret);
        var fragments = new string[shares.Count];
        for (ushort i = 0; i < fragments.Length; ++i)
        {
            fragments[i] = shares[i].ToString();
        }

        return fragments;
    }

    public override byte[]? ComputeFragments(string[] fragments)
    {
        throw new NotImplementedException();
        ArgumentNullException.ThrowIfNull(fragments);

        // var gcd = new SecretSharingDotNet.Math.ExtendedEuclideanAlgorithm<BigInteger>();
        // var combine = new SecretSharingDotNet.Cryptography.ShamirsSecretSharing<BigInteger>(gcd);
        // var shares = string.Join(Environment.NewLine, fragments);
        // var secret = combine.Reconstruction(shares);
        // return secret.ToByteArray();
    }
}