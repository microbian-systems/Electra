namespace Electra.Core.Extensions;

public static class StringExtensions
{
    public static string FromByteArray(this byte[] bytes)
        => Encoding.ASCII.GetString(bytes);
    
    public static byte[] ToByteArray(this string str)
        => Encoding.ASCII.GetBytes(str);
    public static string ToBase64(this string str) 
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
    
    public static string FromBase64(this string str) 
        => Encoding.UTF8.GetString(Convert.FromBase64String(str));
    
    public static string ToBase64(this byte[] bytes) 
        => Convert.ToBase64String(bytes);
    
    public static byte[] FromBase64ToBytes(this string str)
        => Convert.FromBase64String(str);
}