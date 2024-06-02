namespace Electra.Core.Extensions;

public static class ByteExtensions
{
    public static string ToBase64(this byte[] data) => Convert.ToBase64String(data);
    public static byte[] FromBase64(this string data) => Convert.FromBase64String(data);
}