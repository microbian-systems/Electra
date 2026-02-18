using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Represents a response from the Twitter API containing user data.
/// </summary>
public class UserResponse
{
    /// <summary>
    /// The user data returned by the API.
    /// For single user requests, this contains one user.
    /// For batch requests, this contains multiple users.
    /// </summary>
    [JsonPropertyName("data")]
    public User? Data { get; set; }

    /// <summary>
    /// Additional data requested via expansions.
    /// </summary>
    [JsonPropertyName("includes")]
    public Includes? Includes { get; set; }
}