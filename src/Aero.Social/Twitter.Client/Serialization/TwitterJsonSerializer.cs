using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Serialization;

/// <summary>
/// Provides JSON serialization options for Twitter API responses.
/// </summary>
public static class TwitterJsonSerializer
{
    /// <summary>
    /// Gets the default JSON serializer options for Twitter API.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object.</returns>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The JSON string representation.</returns>
    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, DefaultOptions);
    }
}