using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Client.Clients;

/// <summary>
/// Interface for the Twitter API client.
/// </summary>
public interface ITwitterClient
{
    /// <summary>
    /// Retrieves a tweet by its ID.
    /// </summary>
    /// <param name="tweetId">The unique identifier of the tweet.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The requested tweet.</returns>
    Task<Tweet> GetTweetAsync(string tweetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="fields">Optional. Specifies which user fields to include in the response.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The requested user.</returns>
    Task<User> GetUserByIdAsync(string userId, UserFields? fields = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their username.
    /// </summary>
    /// <param name="username">The username (screen name) of the user.</param>
    /// <param name="fields">Optional. Specifies which user fields to include in the response.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The requested user.</returns>
    Task<User> GetUserByUsernameAsync(string username, UserFields? fields = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for recent tweets matching the specified query.
    /// </summary>
    /// <param name="query">The search query. See Twitter API documentation for query syntax.</param>
    /// <param name="options">Optional. Search options including pagination, time ranges, and field selection.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A response containing the matching tweets and pagination metadata.</returns>
    Task<TweetResponse> SearchTweetsAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tweets posted by a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="options">Optional. Timeline options including pagination and field selection.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A response containing the user's tweets and pagination metadata.</returns>
    Task<TweetResponse> GetUserTweetsAsync(string userId, TimelineOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves mentions of a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="options">Optional. Timeline options including pagination and field selection.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A response containing tweets mentioning the user and pagination metadata.</returns>
    Task<TweetResponse> GetUserMentionsAsync(string userId, TimelineOptions? options = null, CancellationToken cancellationToken = default);
}