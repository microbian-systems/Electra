using System.Text.Json.Serialization;

namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Represents a user from the Twitter API.
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// The name of the user, as they've defined it on their profile.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The Twitter handle (screen name) of the user.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// The UTC datetime that the user account was created on Twitter.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The text of this user's profile description (bio), if the user provided one.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The location specified in the user's profile, if the user provided one.
    /// </summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    /// <summary>
    /// The URL to the profile image for this user.
    /// </summary>
    [JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; set; }

    /// <summary>
    /// Indicates if this user is a verified Twitter user.
    /// </summary>
    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    /// <summary>
    /// The URL specified in the user's profile, if provided.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Indicates if the user has a Twitter Blue subscription.
    /// </summary>
    [JsonPropertyName("verified_type")]
    public string? VerifiedType { get; set; }

    /// <summary>
    /// Contains metadata about the user's activity.
    /// </summary>
    [JsonPropertyName("public_metrics")]
    public UserPublicMetrics? PublicMetrics { get; set; }
}