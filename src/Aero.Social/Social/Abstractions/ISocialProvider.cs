using Aero.Social.Models;

namespace Aero.Social.Abstractions;

/// <summary>
/// Defines the contract for social media provider implementations.
/// All social media platforms must implement this interface to enable posting, authentication, and analytics.
/// </summary>
public interface ISocialProvider
{
    /// <summary>
    /// Gets the unique identifier for this provider (e.g., "discord", "twitter", "linkedin").
    /// </summary>
    string Identifier { get; }

    /// <summary>
    /// Gets the display name for this provider (e.g., "Discord", "X (Twitter)", "LinkedIn").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the OAuth scopes required by this provider.
    /// </summary>
    string[] Scopes { get; }

    /// <summary>
    /// Gets the editor type supported by this provider (Normal, Markdown, or HTML).
    /// </summary>
    EditorType Editor { get; }

    /// <summary>
    /// Gets a value indicating whether this provider requires an intermediate step between OAuth and posting.
    /// Used for providers that need additional user selection (e.g., selecting a LinkedIn Page vs Profile).
    /// </summary>
    bool IsBetweenSteps { get; }

    /// <summary>
    /// Gets a value indicating whether this provider uses Web3 authentication (e.g., Nostr, Farcaster).
    /// </summary>
    bool IsWeb3 { get; }

    /// <summary>
    /// Gets the maximum number of concurrent posting jobs allowed for this provider.
    /// </summary>
    int MaxConcurrentJobs { get; }

    /// <summary>
    /// Gets an optional tooltip to display in the UI for this provider.
    /// </summary>
    string? Tooltip { get; }

    /// <summary>
    /// Gets a value indicating whether this provider uses a one-time token instead of refreshable tokens.
    /// </summary>
    bool OneTimeToken { get; }

    /// <summary>
    /// Gets a value indicating whether to wait before refreshing tokens.
    /// </summary>
    bool RefreshWait { get; }

    /// <summary>
    /// Gets a value indicating whether media should be converted to JPEG format before uploading.
    /// </summary>
    bool ConvertToJpeg { get; }
    
    /// <summary>
    /// Gets the maximum content length allowed for posts on this provider.
    /// </summary>
    /// <param name="additionalSettings">Optional settings that may affect the maximum length.</param>
    /// <returns>The maximum number of characters allowed.</returns>
    int MaxLength(object? additionalSettings = null);
    
    /// <summary>
    /// Posts content to the social media platform.
    /// </summary>
    /// <param name="id">The user or account identifier.</param>
    /// <param name="accessToken">The OAuth access token.</param>
    /// <param name="posts">The list of post details to publish.</param>
    /// <param name="integration">The integration configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of post responses containing the result of each post.</returns>
    Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a comment to an existing post.
    /// </summary>
    /// <param name="id">The user or account identifier.</param>
    /// <param name="postId">The ID of the post to comment on.</param>
    /// <param name="lastCommentId">The ID of the last comment in the thread (for threaded replies).</param>
    /// <param name="accessToken">The OAuth access token.</param>
    /// <param name="posts">The list of comment details to publish.</param>
    /// <param name="integration">The integration configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of post responses, or null if commenting is not supported.</returns>
    Task<PostResponse[]?> CommentAsync(
        string id,
        string postId,
        string? lastCommentId,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the OAuth authorization URL for the user to visit.
    /// </summary>
    /// <param name="clientInformation">Optional client-specific information for multi-tenant scenarios.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authorization URL and associated state information.</returns>
    Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens.
    /// </summary>
    /// <param name="parameters">The authentication parameters including the authorization code.</param>
    /// <param name="clientInformation">Optional client-specific information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authentication token details including access token and user information.</returns>
    Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication token details.</returns>
    Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconnects an integration that requires additional authentication steps.
    /// </summary>
    /// <param name="id">The integration ID.</param>
    /// <param name="requiredId">The required ID for reconnection (e.g., page ID).</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated authentication token details, or null if not supported.</returns>
    Task<AuthTokenDetails?> ReConnectAsync(
        string id,
        string requiredId,
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves analytics data for the user's account.
    /// </summary>
    /// <param name="id">The user or account identifier.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="days">Number of days of analytics to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics data arrays, or null if not supported.</returns>
    Task<AnalyticsData[]?> AnalyticsAsync(
        string id,
        string accessToken,
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves analytics data for a specific post.
    /// </summary>
    /// <param name="integrationId">The integration ID.</param>
    /// <param name="accessToken">The access token.</param>
    /// <param name="postId">The post ID to get analytics for.</param>
    /// <param name="days">Number of days of analytics to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics data arrays, or null if not supported.</returns>
    Task<AnalyticsData[]?> PostAnalyticsAsync(
        string integrationId,
        string accessToken,
        string postId,
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for users to mention in posts.
    /// </summary>
    /// <param name="token">The access token.</param>
    /// <param name="query">The mention search query.</param>
    /// <param name="id">The user or account identifier.</param>
    /// <param name="integration">The integration configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of mentionable users, or null if not supported.</returns>
    Task<object?> MentionAsync(
        string token,
        MentionQuery query,
        string id,
        Integration integration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Formats a mention string for this provider.
    /// </summary>
    /// <param name="idOrHandle">The user ID or handle.</param>
    /// <param name="name">The user's display name.</param>
    /// <returns>The formatted mention string, or null if not supported.</returns>
    string? MentionFormat(string idOrHandle, string name);

    /// <summary>
    /// Fetches additional page information for providers that support it.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="data">Additional data for the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Page information result, or null if not supported.</returns>
    Task<FetchPageInformationResult?> FetchPageInformationAsync(
        string accessToken,
        object data,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the editor type supported by a social media provider.
/// </summary>
public enum EditorType
{
    /// <summary>
    /// Standard text editor with no special formatting.
    /// </summary>
    Normal,
    
    /// <summary>
    /// Markdown editor supporting Markdown syntax.
    /// </summary>
    Markdown,
    
    /// <summary>
    /// HTML editor supporting HTML content.
    /// </summary>
    Html
}

/// <summary>
/// Parameters for authentication operations.
/// </summary>
/// <param name="Code">The authorization code returned from the OAuth flow.</param>
/// <param name="CodeVerifier">The PKCE code verifier (if applicable).</param>
/// <param name="Refresh">Optional refresh indicator for refresh token flows.</param>
public record AuthenticateParams(
    string Code,
    string CodeVerifier,
    string? Refresh = null
);

/// <summary>
/// Query for searching mentionable users.
/// </summary>
/// <param name="Query">The search query string.</param>
public record MentionQuery(string Query);
