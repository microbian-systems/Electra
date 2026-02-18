using System.IO;
using System.IO.Compression;

namespace Aero.Common.Extensions;

public static class StreamExtensions
{
    /// <summary>
    /// A helper method to return a compressed version of a MemoryStream
    /// </summary>
    /// <param name="ms"></param>
    /// <returns></returns>
    public static MemoryStream Compress(this MemoryStream ms)
    {
        // Compress
        var compressedMemoryStream = new MemoryStream();
        var gzipStream = new GZipStream(compressedMemoryStream, CompressionMode.Compress);
        gzipStream.Write(ms.ToArray(), 0, (int)ms.Length);
        gzipStream.Close();
        return compressedMemoryStream;
    }

    public static  MemoryStream LoadStreamWithJson(this MemoryStream ms, string json)
    {
        var sw = new StreamWriter(ms);
        sw.Write(json);
        sw.Flush();
        ms.Position = 0;
        return ms;
    }
        
    public static string StripTrailingBackSlash(this string path) => path.TrimEnd('/');
}