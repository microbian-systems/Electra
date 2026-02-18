using System.Net.Http.Headers;

namespace Aero.Social.Twitter.Client.RateLimit;

/// <summary>
/// Parses rate limit headers from Twitter API responses.
/// </summary>
public static class RateLimitParser
{
    private const string RateLimitLimitHeader = "X-Rate-Limit-Limit";
    private const string RateLimitRemainingHeader = "X-Rate-Limit-Remaining";
    private const string RateLimitResetHeader = "X-Rate-Limit-Reset";
    private const string RetryAfterHeader = "Retry-After";

    /// <summary>
    /// Parses rate limit information from the response headers.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>The parsed rate limit information, or null if headers are not present.</returns>
    public static RateLimitInfo? ParseRateLimitHeaders(HttpResponseMessage response)
    {
        if (response == null)
        {
            return null;
        }

        var info = new RateLimitInfo();
        var headers = response.Headers;

        // Parse X-Rate-Limit-Limit
        if (TryGetHeaderValue(headers, RateLimitLimitHeader, out var limitValue) &&
            int.TryParse(limitValue, out var limit))
        {
            info.Limit = limit;
        }

        // Parse X-Rate-Limit-Remaining
        if (TryGetHeaderValue(headers, RateLimitRemainingHeader, out var remainingValue) &&
            int.TryParse(remainingValue, out var remaining))
        {
            info.Remaining = remaining;
        }

        // Parse X-Rate-Limit-Reset
        if (TryGetHeaderValue(headers, RateLimitResetHeader, out var resetValue) &&
            long.TryParse(resetValue, out var resetTimestamp))
        {
            info.ResetTimestamp = resetTimestamp;
        }

        // Parse Retry-After (for 429 responses)
        if (TryGetHeaderValue(headers, RetryAfterHeader, out var retryAfterValue) &&
            int.TryParse(retryAfterValue, out var retryAfterSeconds))
        {
            info.RetryAfter = TimeSpan.FromSeconds(retryAfterSeconds);
        }

        // Return null if no rate limit headers were found (including Retry-After)
        if (info.Limit == 0 && info.Remaining == 0 && info.ResetTimestamp == 0 && !info.RetryAfter.HasValue)
        {
            return null;
        }

        return info;
    }

    /// <summary>
    /// Attempts to get a header value from the response headers.
    /// </summary>
    private static bool TryGetHeaderValue(HttpResponseHeaders headers, string headerName, out string value)
    {
        value = string.Empty;

        if (headers.TryGetValues(headerName, out var values))
        {
            value = values.FirstOrDefault() ?? string.Empty;
            return !string.IsNullOrEmpty(value);
        }

        return false;
    }

    /// <summary>
    /// Gets a human-readable description of the rate limit status.
    /// </summary>
    /// <param name="info">The rate limit information.</param>
    /// <returns>A descriptive string about the rate limit status.</returns>
    public static string GetRateLimitDescription(RateLimitInfo? info)
    {
        if (info == null)
        {
            return "Rate limit information not available.";
        }

        if (info.IsRateLimited)
        {
            return $"Rate limit exceeded. Resets at {info.ResetTime:HH:mm:ss UTC} ({info.TimeUntilReset.TotalMinutes:F1} minutes).";
        }

        if (info.IsApproachingLimit)
        {
            return $"Approaching rate limit. {info.Remaining} of {info.Limit} requests remaining ({info.PercentConsumed:F0}% consumed). Resets in {info.TimeUntilReset.TotalMinutes:F1} minutes.";
        }

        return $"{info.Remaining} of {info.Limit} requests remaining ({info.PercentConsumed:F0}% consumed). Resets in {info.TimeUntilReset.TotalMinutes:F1} minutes.";
    }

    /// <summary>
    /// Determines if a rate limit warning should be logged based on remaining percentage.
    /// </summary>
    /// <param name="info">The rate limit information.</param>
    /// <returns>True if a warning should be logged; otherwise, false.</returns>
    public static bool ShouldLogWarning(RateLimitInfo? info)
    {
        if (info == null)
        {
            return false;
        }

        // Log warning if less than 10% remaining or if rate limited
        return info.IsRateLimited || (info.Limit > 0 && info.Remaining < info.Limit * 0.1);
    }
}