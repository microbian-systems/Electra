using System.Net;

namespace Aero.Social.Twitter.Client.Errors;

/// <summary>
/// Provides enhanced error information for Twitter API errors.
/// </summary>
public static class TwitterErrorInfo
{
    private static readonly Dictionary<int, (string Title, string Action, string DocUrl)> ErrorCodeMap = new()
    {
        // Client Errors (4xx)
        [3] = ("Invalid coordinates", "Verify the coordinate format (latitude, longitude) and ensure values are within valid ranges.", "https://developer.twitter.com/en/docs/twitter-api/v1/data-dictionary/object-model/geo"),
        [13] = ("No location associated with the specified IP address", "This error occurs when the IP address cannot be geolocated. No action required.", "https://developer.twitter.com/en/docs/twitter-api/v1/accounts-and-users/manage-account-settings/api-reference/get-account-settings"),
        [17] = ("No user matches for specified terms", "Check your query parameters and ensure the user exists.", "https://developer.twitter.com/en/docs/twitter-api/v1/accounts-and-users/follow-search-get-users/api-reference/get-users-lookup"),
        [32] = ("Could not authenticate you", "Your authentication credentials are missing or incorrect. Verify your API keys and tokens.", "https://developer.twitter.com/en/docs/authentication/guides/authentication-best-practices"),
        [34] = ("Sorry, that page does not exist", "The requested resource was not found. Check the endpoint URL and resource ID.", "https://developer.twitter.com/en/docs/twitter-api/api-reference-index"),
        [36] = ("You cannot report yourself for spam", "You attempted to report yourself. This action is not allowed.", "https://developer.twitter.com/en/docs/twitter-api/v1/accounts-and-users/mute-block-report-users/api-reference/post-users-report_spam"),
        [38] = ("Missing parameter", "A required parameter is missing from your request. Check the API documentation for required fields.", "https://developer.twitter.com/en/docs/twitter-api/api-reference-index"),
        [44] = ("attachment_url parameter is invalid", "The URL provided is not valid for this endpoint. Ensure it's a Twitter URL.", "https://developer.twitter.com/en/docs/twitter-api/v1/tweets/post-and-engage/api-reference/post-statuses-update"),
        [50] = ("User not found", "The specified user could not be found. Check the username or user ID.", "https://developer.twitter.com/en/docs/twitter-api/v1/accounts-and-users/follow-search-get-users/overview"),
        [63] = ("User has been suspended", "The user account has been suspended. You cannot interact with this account.", "https://help.twitter.com/en/rules-and-policies/enforcement-options"),
        [64] = ("Your account is suspended and is not permitted to access this feature", "Your account has been suspended. Contact Twitter support for assistance.", "https://help.twitter.com/en/forms/account-access/appeals"),
        [68] = ("The Twitter REST API v1 is no longer active", "Use the Twitter API v2 endpoints instead.", "https://developer.twitter.com/en/docs/twitter-api/migrate/twitter-api-endpoint-map"),
        [88] = ("Rate limit exceeded", "You have exceeded the rate limit. Wait before making additional requests.", "https://developer.twitter.com/en/docs/twitter-api/rate-limits"),
        [92] = ("SSL is required", "Use HTTPS instead of HTTP for all API requests.", "https://developer.twitter.com/en/docs/twitter-api/v1/developer-utilities/status-update-with-media"),
        [130] = ("Over capacity", "Twitter is currently over capacity. Please try again later.", "https://developer.twitter.com/en/docs/twitter-api/v1/troubleshooting"),
        [131] = ("Internal error", "An internal Twitter error occurred. Please try again later.", "https://developer.twitter.com/en/docs/twitter-api/v1/troubleshooting"),
        [135] = ("Could not authenticate you", "Your credentials do not allow access to this resource. Check your app permissions.", "https://developer.twitter.com/en/docs/authentication/guides/authentication-best-practices"),
        [144] = ("No status found with that ID", "The tweet ID you specified does not exist or has been deleted.", "https://developer.twitter.com/en/docs/twitter-api/v1/tweets/post-and-engage/api-reference/get-statuses-show-id"),
        [150] = ("Cannot send direct messages to users who are not following you", "The recipient must follow you before you can send direct messages.", "https://developer.twitter.com/en/docs/twitter-api/v1/direct-messages/sending-and-receiving/overview"),
        [151] = ("Message already sent", "You have already sent this message. Duplicate messages are not allowed.", "https://developer.twitter.com/en/docs/twitter-api/v1/direct-messages/sending-and-receiving/overview"),
        [160] = ("You've already requested to follow this user", "You have already sent a follow request to this user.", "https://developer.twitter.com/en/docs/twitter-api/v1/accounts-and-users/follow-search-get-users/api-reference/post-friendships-create"),
        [161] = ("You are unable to follow more people at this time", "You have reached the follow limit. Unfollow some users before following more.", "https://help.twitter.com/en/using-x/following-and-followers"),
        [179] = ("Sorry, you are not authorized to see this status", "The tweet author's privacy settings prevent you from viewing this tweet.", "https://help.twitter.com/en/safety-and-security/public-and-protected-tweets"),
        [185] = ("User is over daily status update limit", "You have reached the daily tweet limit. Wait before posting more tweets.", "https://help.twitter.com/en/using-x/posting-tweets"),
        [186] = ("Tweet needs to be a bit shorter", "Your tweet exceeds the character limit. Shorten your message.", "https://help.twitter.com/en/using-x/posting-tweets"),
        [187] = ("Status is a duplicate", "You have already posted this exact tweet. Try posting different content.", "https://help.twitter.com/en/using-x/posting-tweets"),
        [190] = ("Status is a duplicate", "This appears to be a duplicate status. Twitter prevents duplicate tweets.", "https://help.twitter.com/en/using-x/posting-tweets"),
        [191] = ("Status is a duplicate", "Your tweet is too similar to a recent tweet. Try making it more unique.", "https://help.twitter.com/en/using-x/posting-tweets"),
        [193] = ("Status is a duplicate", "This tweet has already been posted. Please post unique content.", "https://help.twitter.com/en/using-x/posting-tweets"),
        [200] = ("Forbidden", "You are not authorized to access this resource. Check your permissions.", "https://developer.twitter.com/en/docs/authentication/guides/authentication-best-practices"),
        [214] = ("Bad authentication data", "Your OAuth credentials are incorrect or expired. Regenerate your access tokens.", "https://developer.twitter.com/en/docs/authentication/oauth-1-0a"),
        [215] = ("Bad authentication data", "Authentication credentials are missing. Ensure all OAuth parameters are included.", "https://developer.twitter.com/en/docs/authentication/oauth-1-0a"),
        [220] = ("Your credentials do not allow access to this resource", "Your app permissions do not allow this action. Update your app settings.", "https://developer.twitter.com/en/docs/apps/app-permissions"),
        [226] = ("This request looks like it might be automated", "Twitter detected potentially automated behavior. Slow down your request rate.", "https://developer.twitter.com/en/docs/twitter-api/rate-limits"),
        [231] = ("Must pass either status_id or media_id", "Provide either a status_id or media_id parameter in your request.", "https://developer.twitter.com/en/docs/twitter-api/v1/tweets/post-and-engage/api-reference/post-statuses-retweet-id"),
        [251] = ("This method requires a PUT or POST", "Use PUT or POST instead of GET for this endpoint.", "https://developer.twitter.com/en/docs/twitter-api/api-reference-index"),
        [261] = ("Application cannot perform write actions", "Your app is read-only. Change app permissions in the developer portal.", "https://developer.twitter.com/en/docs/apps/app-permissions"),
        [271] = ("Cannot mute yourself", "You cannot mute your own account.", "https://developer.twitter.com/en/docs/twitter-api/v1/accounts-and-users/mute-block-report-users/api-reference/post-mutes-users-create"),
        [272] = ("You are not muting the specified user", "You cannot unmute a user you are not muting.", "https://developer.twitter.com/en/docs/twitter-api/v1/accounts-and-users/mute-block-report-users/api-reference/post-mutes-users-destroy"),
        [323] = ("Animated GIFs are not allowed when uploading multiple images", "Use either a single animated GIF or multiple static images, not both.", "https://developer.twitter.com/en/docs/twitter-api/v1/media/upload-media/overview"),
        [324] = ("Media IDs are not valid", "The media ID(s) you provided are invalid or expired. Re-upload the media.", "https://developer.twitter.com/en/docs/twitter-api/v1/media/upload-media/overview"),
        [325] = ("A media ID was not found", "One or more media IDs do not exist. Check the media IDs in your request.", "https://developer.twitter.com/en/docs/twitter-api/v1/media/upload-media/overview"),
        [326] = ("To protect our users from spam and other malicious activity, this account is temporarily locked", "Your account has been temporarily locked. Verify your account at twitter.com.", "https://help.twitter.com/en/managing-your-account/locked-and-limited-accounts"),
        [327] = ("You have already retweeted this Tweet", "You have already retweeted this tweet. You cannot retweet it again.", "https://help.twitter.com/en/using-x/posting-tweets"),
        [349] = ("You cannot send messages to this user", "This user does not accept direct messages from you.", "https://help.twitter.com/en/using-x/direct-messages"),
        [354] = ("The text of your direct message is over the max character limit", "Your direct message is too long. Shorten it to fit within the limit.", "https://help.twitter.com/en/using-x/direct-messages"),
        [355] = ("Subscription already exists", "You are already subscribed to this user or list.", "https://developer.twitter.com/en/docs/twitter-api/v1/accounts-and-users/follow-search-get-users/overview"),
        [385] = ("You attempted to reply to a tweet that is deleted or not visible to you", "The tweet you are replying to is not available. It may be deleted or protected.", "https://help.twitter.com/en/using-x/posting-tweets"),
        [386] = ("The tweet exceeds the number of allowed attachment types", "You can only include one type of attachment (poll, quote tweet, or media).", "https://help.twitter.com/en/using-x/posting-tweets"),
        [407] = ("The given URL is invalid", "The URL format is incorrect or the URL is not accessible.", "https://developer.twitter.com/en/docs/twitter-api/v1/tweets/post-and-engage/api-reference/post-statuses-update"),
        [415] = ("Callback URL not approved for this client application", "Add the callback URL to your app's settings in the developer portal.", "https://developer.twitter.com/en/docs/apps/callback-urls"),
        [416] = ("Invalid / suspended application", "The app has been suspended. Contact Twitter developer support.", "https://developer.twitter.com/en/support"),
        [417] = ("Desktop applications only support the oauth_callback value 'oob'", "Use 'oob' for desktop applications without a callback URL.", "https://developer.twitter.com/en/docs/authentication/oauth-1-0a/pin-based-oauth"),

        // Server Errors (5xx)
        [500] = ("Internal Server Error", "Twitter experienced an internal error. Please try again later.", "https://developer.twitter.com/en/docs/twitter-api/v1/troubleshooting"),
        [503] = ("Service Unavailable", "Twitter is temporarily unavailable. Please try again later.", "https://developer.twitter.com/en/docs/twitter-api/v1/troubleshooting"),
        [504] = ("Gateway Timeout", "The request timed out. Please try again later.", "https://developer.twitter.com/en/docs/twitter-api/v1/troubleshooting"),
    };

    /// <summary>
    /// Gets enhanced error information for a Twitter API error code.
    /// </summary>
    /// <param name="code">The Twitter API error code.</param>
    /// <returns>A tuple containing the title, suggested action, and documentation URL.</returns>
    public static (string Title, string Action, string DocumentationUrl) GetErrorInfo(int code)
    {
        if (ErrorCodeMap.TryGetValue(code, out var info))
        {
            return info;
        }

        return ("Unknown Error", "An unexpected error occurred. Please check the Twitter API documentation for more information.", "https://developer.twitter.com/en/docs/twitter-api/v1/troubleshooting");
    }

    /// <summary>
    /// Gets a human-readable title for an error code.
    /// </summary>
    /// <param name="code">The Twitter API error code.</param>
    /// <returns>The error title.</returns>
    public static string GetErrorTitle(int code)
    {
        return GetErrorInfo(code).Title;
    }

    /// <summary>
    /// Gets the suggested action for resolving an error.
    /// </summary>
    /// <param name="code">The Twitter API error code.</param>
    /// <returns>The suggested action.</returns>
    public static string GetSuggestedAction(int code)
    {
        return GetErrorInfo(code).Action;
    }

    /// <summary>
    /// Gets the documentation URL for an error code.
    /// </summary>
    /// <param name="code">The Twitter API error code.</param>
    /// <returns>The documentation URL.</returns>
    public static string GetDocumentationUrl(int code)
    {
        return GetErrorInfo(code).DocumentationUrl;
    }

    /// <summary>
    /// Builds an enhanced error message with context and actionable guidance.
    /// </summary>
    /// <param name="code">The Twitter API error code.</param>
    /// <param name="apiMessage">The message from the API response.</param>
    /// <returns>An enhanced error message.</returns>
    public static string BuildEnhancedMessage(int code, string? apiMessage)
    {
        var (title, action, docUrl) = GetErrorInfo(code);
        var message = $"Twitter API Error {code}: {title}";
            
        if (!string.IsNullOrEmpty(apiMessage))
        {
            message += $"\nAPI Message: {apiMessage}";
        }
            
        message += $"\n\nSuggested Action: {action}";
        message += $"\nDocumentation: {docUrl}";
            
        return message;
    }

    /// <summary>
    /// Determines if an HTTP status code indicates a client error (4xx).
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>True if the status code is a client error; otherwise, false.</returns>
    public static bool IsClientError(HttpStatusCode statusCode)
    {
        int code = (int)statusCode;
        return code >= 400 && code < 500;
    }

    /// <summary>
    /// Determines if an HTTP status code indicates a server error (5xx).
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>True if the status code is a server error; otherwise, false.</returns>
    public static bool IsServerError(HttpStatusCode statusCode)
    {
        int code = (int)statusCode;
        return code >= 500 && code < 600;
    }

    /// <summary>
    /// Determines if an HTTP status code indicates a rate limit error (429).
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>True if the status code is a rate limit error; otherwise, false.</returns>
    public static bool IsRateLimitError(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.TooManyRequests;
    }
}