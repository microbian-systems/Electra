namespace Electra.Core.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Converts a string to a Base64 string.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The Base64 representation of the input string.</returns>
    public static string ToBase64(this string str)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(str));

    /// <summary>
    /// Converts a Base64 string back to a regular string.
    /// </summary>
    /// <param name="str">The Base64 string to convert.</param>
    /// <returns>The original string that was converted to Base64.</returns>
    public static string FromBase64(this string str)
        => Encoding.UTF8.GetString(Convert.FromBase64String(str));

    /// <summary>
    /// Converts a byte array to a Base64 string.
    /// </summary>
    /// <param name="bytes">The byte array to convert.</param>
    /// <returns>The Base64 representation of the input byte array.</returns>
    public static string ToBase64(this byte[] bytes)
        => Convert.ToBase64String(bytes);

    /// <summary>
    /// Converts a Base64 string back to a byte array.
    /// </summary>
    /// <param name="str">The Base64 string to convert.</param>
    /// <returns>The original byte array that was converted to Base64.</returns>
    public static byte[] FromBase64ToBytes(this string str)
        => Convert.FromBase64String(str);
}