namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Specifies which user fields to include in the API response.
/// </summary>
[Flags]
public enum UserFields
{
    /// <summary>
    /// No additional fields requested.
    /// </summary>
    None = 0,

    /// <summary>
    /// The UTC datetime that the user account was created on Twitter.
    /// </summary>
    CreatedAt = 1 << 0,

    /// <summary>
    /// The text of this user's profile description (bio), if the user provided one.
    /// </summary>
    Description = 1 << 1,

    /// <summary>
    /// Entities that have been parsed out of the url or description fields.
    /// </summary>
    Entities = 1 << 2,

    /// <summary>
    /// The location specified in the user's profile, if the user provided one.
    /// </summary>
    Location = 1 << 3,

    /// <summary>
    /// Unique identifier of this user's pinned Tweet.
    /// </summary>
    PinnedTweetId = 1 << 4,

    /// <summary>
    /// The URL to the profile image for this user.
    /// </summary>
    ProfileImageUrl = 1 << 5,

    /// <summary>
    /// Indicates if this user is a verified Twitter user.
    /// </summary>
    Verified = 1 << 6,

    /// <summary>
    /// The URL specified in the user's profile, if provided.
    /// </summary>
    Url = 1 << 7,

    /// <summary>
    /// Contains metadata about the user's activity.
    /// </summary>
    PublicMetrics = 1 << 8,

    /// <summary>
    /// The Twitter handle (screen name) of the user.
    /// </summary>
    Username = 1 << 9,

    /// <summary>
    /// Indicates if the user has a Twitter Blue subscription.
    /// </summary>
    VerifiedType = 1 << 10,

    /// <summary>
    /// All available user fields.
    /// </summary>
    All = CreatedAt | Description | Entities | Location | PinnedTweetId | ProfileImageUrl | Verified | Url | PublicMetrics | Username | VerifiedType
}

/// <summary>
/// Extension methods for UserFields enum.
/// </summary>
public static class UserFieldsExtensions
{
    /// <summary>
    /// Converts the UserFields flags to a comma-separated string for API requests.
    /// </summary>
    public static string ToApiString(this UserFields fields)
    {
        if (fields == UserFields.None)
            return string.Empty;

        var parts = new System.Collections.Generic.List<string>();

        if (fields.HasFlag(UserFields.CreatedAt))
            parts.Add("created_at");
        if (fields.HasFlag(UserFields.Description))
            parts.Add("description");
        if (fields.HasFlag(UserFields.Entities))
            parts.Add("entities");
        if (fields.HasFlag(UserFields.Location))
            parts.Add("location");
        if (fields.HasFlag(UserFields.PinnedTweetId))
            parts.Add("pinned_tweet_id");
        if (fields.HasFlag(UserFields.ProfileImageUrl))
            parts.Add("profile_image_url");
        if (fields.HasFlag(UserFields.Verified))
            parts.Add("verified");
        if (fields.HasFlag(UserFields.Url))
            parts.Add("url");
        if (fields.HasFlag(UserFields.PublicMetrics))
            parts.Add("public_metrics");
        if (fields.HasFlag(UserFields.Username))
            parts.Add("username");
        if (fields.HasFlag(UserFields.VerifiedType))
            parts.Add("verified_type");

        return string.Join(",", parts);
    }
}