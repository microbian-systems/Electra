namespace Aero.Social.Twitter.Client.RateLimit;

/// <summary>
/// Contains rate limit information from the Twitter API response headers.
/// </summary>
public class RateLimitInfo
{
    /// <summary>
    /// Gets or sets the rate limit ceiling for the given request.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets the number of requests left for the 15-minute window.
    /// </summary>
    public int Remaining { get; set; }

    /// <summary>
    /// Gets or sets the UTC Unix timestamp when the rate limit window resets.
    /// </summary>
    public long ResetTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the time to wait before retrying the request (for 429 responses).
    /// </summary>
    public TimeSpan? RetryAfter { get; set; }

    /// <summary>
    /// Gets the UTC DateTime when the rate limit window resets.
    /// </summary>
    public DateTimeOffset ResetTime => DateTimeOffset.FromUnixTimeSeconds(ResetTimestamp);

    /// <summary>
    /// Gets a value indicating whether the rate limit has been exceeded.
    /// </summary>
    public bool IsRateLimited => Remaining == 0;

    /// <summary>
    /// Gets the percentage of the rate limit that has been consumed.
    /// </summary>
    public double PercentConsumed => Limit > 0 ? ((Limit - Remaining) / (double)Limit) * 100 : 0;

    /// <summary>
    /// Gets a value indicating whether the rate limit is approaching (less than 20% remaining).
    /// </summary>
    public bool IsApproachingLimit => Remaining > 0 && Remaining < Limit * 0.2;

    /// <summary>
    /// Gets the time remaining until the rate limit window resets.
    /// </summary>
    public TimeSpan TimeUntilReset => ResetTime > DateTimeOffset.UtcNow ? ResetTime - DateTimeOffset.UtcNow : TimeSpan.Zero;
}