using System.Security.Cryptography;

namespace Electra.Common;

public static class Utils
{
    /// <summary>
    /// Gets the gravatar URL from the given parameters.
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="size">The requested size</param>
    /// <returns>The gravatar URL</returns>
    public static string GetGravatarUrl(string email, int size = 0)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(email));

        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2"));
        }

        return "https://www.gravatar.com/avatar/" + sb.ToString().ToLower() +
               (size > 0 ? $"?s={size}" : string.Empty);
    }
}