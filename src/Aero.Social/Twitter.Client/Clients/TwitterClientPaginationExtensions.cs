using System.Runtime.CompilerServices;
using Aero.Social.Twitter.Client.Exceptions;
using Aero.Social.Twitter.Client.Models;

namespace Aero.Social.Twitter.Client.Clients;

/// <summary>
/// Extension methods for TwitterClient that provide IAsyncEnumerable support for paginated results.
/// </summary>
public static class TwitterClientPaginationExtensions
{
    /// <summary>
    /// Searches for tweets matching the specified query with automatic pagination.
    /// </summary>
    /// <param name="client">The Twitter client.</param>
    /// <param name="query">The search query.</param>
    /// <param name="options">Optional search options.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of tweets.</returns>
    public static async IAsyncEnumerable<Tweet> SearchTweetsPaginatedAsync(
        this ITwitterClient client,
        string query,
        SearchOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        string? nextToken = null;
        var currentOptions = options ?? new SearchOptions();

        do
        {
            // Create a new options instance with the next token
            var pageOptions = new SearchOptions
            {
                MaxResults = currentOptions.MaxResults,
                SinceId = currentOptions.SinceId,
                UntilId = currentOptions.UntilId,
                StartTime = currentOptions.StartTime,
                EndTime = currentOptions.EndTime,
                NextToken = nextToken,
                TweetFields = currentOptions.TweetFields,
                Expansions = currentOptions.Expansions,
                UserFields = currentOptions.UserFields
            };

            TweetResponse response;
            try
            {
                response = await client.SearchTweetsAsync(query, pageOptions, cancellationToken);
            }
            catch (TwitterRateLimitException ex) when (ex.RetryAfter.HasValue)
            {
                // Wait for rate limit to reset before continuing
                await Task.Delay(ex.RetryAfter.Value, cancellationToken);
                continue;
            }

            if (response.Data != null)
            {
                foreach (var tweet in response.Data)
                {
                    yield return tweet;
                }
            }

            nextToken = response.Meta?.NextToken;

        } while (!string.IsNullOrEmpty(nextToken) && !cancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// Retrieves tweets posted by a specific user with automatic pagination.
    /// </summary>
    /// <param name="client">The Twitter client.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="options">Optional timeline options.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of tweets.</returns>
    public static async IAsyncEnumerable<Tweet> GetUserTweetsPaginatedAsync(
        this ITwitterClient client,
        string userId,
        TimelineOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        string? paginationToken = null;
        var currentOptions = options ?? new TimelineOptions();

        do
        {
            // Create a new options instance with the pagination token
            var pageOptions = new TimelineOptions
            {
                MaxResults = currentOptions.MaxResults,
                SinceId = currentOptions.SinceId,
                UntilId = currentOptions.UntilId,
                StartTime = currentOptions.StartTime,
                EndTime = currentOptions.EndTime,
                PaginationToken = paginationToken,
                Exclude = currentOptions.Exclude,
                TweetFields = currentOptions.TweetFields,
                Expansions = currentOptions.Expansions,
                UserFields = currentOptions.UserFields
            };

            TweetResponse response;
            try
            {
                response = await client.GetUserTweetsAsync(userId, pageOptions, cancellationToken);
            }
            catch (TwitterRateLimitException ex) when (ex.RetryAfter.HasValue)
            {
                // Wait for rate limit to reset before continuing
                await Task.Delay(ex.RetryAfter.Value, cancellationToken);
                continue;
            }

            if (response.Data != null)
            {
                foreach (var tweet in response.Data)
                {
                    yield return tweet;
                }
            }

            paginationToken = response.Meta?.NextToken;

        } while (!string.IsNullOrEmpty(paginationToken) && !cancellationToken.IsCancellationRequested);
    }

    /// <summary>
    /// Retrieves mentions of a specific user with automatic pagination.
    /// </summary>
    /// <param name="client">The Twitter client.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="options">Optional timeline options.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of tweets.</returns>
    public static async IAsyncEnumerable<Tweet> GetUserMentionsPaginatedAsync(
        this ITwitterClient client,
        string userId,
        TimelineOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        string? paginationToken = null;
        var currentOptions = options ?? new TimelineOptions();

        do
        {
            // Create a new options instance with the pagination token
            var pageOptions = new TimelineOptions
            {
                MaxResults = currentOptions.MaxResults,
                SinceId = currentOptions.SinceId,
                UntilId = currentOptions.UntilId,
                StartTime = currentOptions.StartTime,
                EndTime = currentOptions.EndTime,
                PaginationToken = paginationToken,
                TweetFields = currentOptions.TweetFields,
                Expansions = currentOptions.Expansions,
                UserFields = currentOptions.UserFields
            };

            TweetResponse response;
            try
            {
                response = await client.GetUserMentionsAsync(userId, pageOptions, cancellationToken);
            }
            catch (TwitterRateLimitException ex) when (ex.RetryAfter.HasValue)
            {
                // Wait for rate limit to reset before continuing
                await Task.Delay(ex.RetryAfter.Value, cancellationToken);
                continue;
            }

            if (response.Data != null)
            {
                foreach (var tweet in response.Data)
                {
                    yield return tweet;
                }
            }

            paginationToken = response.Meta?.NextToken;

        } while (!string.IsNullOrEmpty(paginationToken) && !cancellationToken.IsCancellationRequested);
    }
}