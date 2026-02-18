using System.IO;
using System.IO.Compression;

namespace Aero.Common.Extensions;

// todo - create unit tests for Aero.Core.CompressionHelpers
public static class Compression
{
    public static byte[] Compress(byte[] data)
    {
        byte[] compressArray = null;

        using (var memoryStream = new MemoryStream())
        using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
        {
            deflateStream.Write(data, 0, data.Length);
            compressArray = memoryStream.ToArray();
        }

        return compressArray;
    }

    public static byte[] Decompress(byte[] data)
    {
        byte[] decompressedArray = null;

        using (var decompressedStream = new MemoryStream())
        using (var compressStream = new MemoryStream(data))
        using (var deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress))
        {
            deflateStream.CopyTo(decompressedStream);
            decompressedArray = decompressedStream.ToArray();
        }

        return decompressedArray;
    }
}