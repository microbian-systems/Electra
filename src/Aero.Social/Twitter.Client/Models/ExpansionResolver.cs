namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Provides utility methods for resolving Twitter API expansions from Includes data.
/// </summary>
public static class ExpansionResolver
{
    /// <summary>
    /// Resolves the author of a tweet from the includes data.
    /// </summary>
    /// <param name="tweet">The tweet to resolve the author for.</param>
    /// <param name="includes">The includes data containing user information.</param>
    /// <returns>The user who authored the tweet, or null if not found.</returns>
    public static User? ResolveAuthor(this Tweet tweet, Includes? includes)
    {
        if (tweet?.AuthorId == null || includes?.Users == null)
        {
            return null;
        }

        return includes.Users.FirstOrDefault(u => u.Id == tweet.AuthorId);
    }

    /// <summary>
    /// Resolves a user by ID from the includes data.
    /// </summary>
    /// <param name="includes">The includes data.</param>
    /// <param name="userId">The user ID to resolve.</param>
    /// <returns>The user with the specified ID, or null if not found.</returns>
    public static User? ResolveUser(this Includes? includes, string? userId)
    {
        if (string.IsNullOrEmpty(userId) || includes?.Users == null)
        {
            return null;
        }

        return includes.Users.FirstOrDefault(u => u.Id == userId);
    }

    /// <summary>
    /// Resolves a tweet by ID from the includes data.
    /// </summary>
    /// <param name="includes">The includes data.</param>
    /// <param name="tweetId">The tweet ID to resolve.</param>
    /// <returns>The tweet with the specified ID, or null if not found.</returns>
    public static Tweet? ResolveTweet(this Includes? includes, string? tweetId)
    {
        if (string.IsNullOrEmpty(tweetId) || includes?.Tweets == null)
        {
            return null;
        }

        return includes.Tweets.FirstOrDefault(t => t.Id == tweetId);
    }

    /// <summary>
    /// Resolves media by its media key from the includes data.
    /// </summary>
    /// <param name="includes">The includes data.</param>
    /// <param name="mediaKey">The media key to resolve.</param>
    /// <returns>The media with the specified key, or null if not found.</returns>
    public static Media? ResolveMedia(this Includes? includes, string? mediaKey)
    {
        if (string.IsNullOrEmpty(mediaKey) || includes?.Media == null)
        {
            return null;
        }

        return includes.Media.FirstOrDefault(m => m.MediaKey == mediaKey);
    }

    /// <summary>
    /// Resolves multiple media objects by their media keys from the includes data.
    /// </summary>
    /// <param name="includes">The includes data.</param>
    /// <param name="mediaKeys">The media keys to resolve.</param>
    /// <returns>A list of media objects matching the keys.</returns>
    public static List<Media> ResolveMedia(this Includes? includes, IEnumerable<string>? mediaKeys)
    {
        if (mediaKeys == null || includes?.Media == null)
        {
            return new List<Media>();
        }

        var keySet = new HashSet<string>(mediaKeys);
        return includes.Media.Where(m => m.MediaKey != null && keySet.Contains(m.MediaKey)).ToList();
    }

    /// <summary>
    /// Resolves all users mentioned in entity mentions from the includes data.
    /// </summary>
    /// <param name="includes">The includes data.</param>
    /// <param name="usernames">The usernames to resolve.</param>
    /// <returns>A list of users matching the usernames.</returns>
    public static List<User> ResolveUsersByUsername(this Includes? includes, IEnumerable<string>? usernames)
    {
        if (usernames == null || includes?.Users == null)
        {
            return new List<User>();
        }

        var usernameSet = new HashSet<string>(usernames);
        return includes.Users.Where(u => u.Username != null && usernameSet.Contains(u.Username)).ToList();
    }
}