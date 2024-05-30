using System.Security.Cryptography;
using ThrowGuard;

namespace Electra.Core.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    /// Serialize the object to json string
    /// </summary>
    /// <param name="obj">the object to be serialized</param>
    /// <returns>a json string representation of the object</returns>
    public static string ToJson(this object obj) 
        => JsonSerializer.Serialize(obj);

    /// <summary>
    /// Deserialize the object from the json string
    /// </summary>
    /// <param name="json">the json to be deserialized</param>
    /// <typeparam name="T">the type to be deserialized</typeparam>
    /// <returns>object of type T</returns>
    public static T? FromJson<T>(string json) where T : class
        => JsonSerializer.Deserialize<T>(json);

    /// <summary>
    /// Get the MD5 hash of the object
    /// </summary>
    /// <param name="obj">the object to be hashed</param>
    /// <returns>the hash of the object</returns>
    public static string GetMd5Hash(this object obj)
    {
        Throw.IfNull(obj, "{0} cannot be null", nameof(obj));
        // Serialize the expression to a string
        var serialized = obj.ToString();
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(serialized!));
        var builder = new StringBuilder();
        
        foreach (var t in bytes)
            builder.Append(t.ToString("x2"));
        
        return builder.ToString();
    }

    /// <summary>
    /// Get the SHA256 hash of the object
    /// </summary>
    /// <param name="obj">the object to be hashed</param>
    /// <returns>the hash of the object</returns>
    public static string GetSha256Hash(this object obj)
    {
        Throw.IfNull(obj, "{0} cannot be null", nameof(obj));
        // Serialize the expression to a string
        var serialized = obj.ToString();

        // Compute the hash
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(serialized!));
        var builder = new StringBuilder();
        
        foreach (var @byte in bytes)
            builder.Append(@byte.ToString("x2"));
        
        return builder.ToString();
    }
    
}