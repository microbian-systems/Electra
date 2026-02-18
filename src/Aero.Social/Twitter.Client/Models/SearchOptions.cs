namespace Aero.Social.Twitter.Client.Models;

/// <summary>
/// Options for searching tweets via the Twitter API.
/// </summary>
public class SearchOptions
{
    /// <summary>
    /// The maximum number of results to return per page.
    /// Valid values: 10-100. Default: 10.
    /// </summary>
    public int? MaxResults { get; set; }

    /// <summary>
    /// Returns results with a tweet ID greater than (more recent than) the specified ID.
    /// </summary>
    public string? SinceId { get; set; }

    /// <summary>
    /// Returns results with a tweet ID less than (older than) the specified ID.
    /// </summary>
    public string? UntilId { get; set; }

    /// <summary>
    /// Returns tweets created after the specified datetime.
    /// </summary>
    public DateTimeOffset? StartTime { get; set; }

    /// <summary>
    /// Returns tweets created before the specified datetime.
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// The pagination token for navigating through result pages.
    /// </summary>
    public string? NextToken { get; set; }

    /// <summary>
    /// Specifies which tweet fields to include in the response.
    /// </summary>
    public TweetFields? TweetFields { get; set; }

    /// <summary>
    /// Specifies which expansions to include in the response.
    /// </summary>
    public ExpansionOptions? Expansions { get; set; }

    /// <summary>
    /// Specifies which user fields to include in the response (when using expansions).
    /// </summary>
    public UserFields? UserFields { get; set; }

    /// <summary>
    /// Validates the search options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (MaxResults.HasValue)
        {
            if (MaxResults.Value < 10 || MaxResults.Value > 100)
            {
                throw new ArgumentException("MaxResults must be between 10 and 100", nameof(MaxResults));
            }
        }

        if (StartTime.HasValue && EndTime.HasValue)
        {
            if (StartTime.Value > EndTime.Value)
            {
                throw new ArgumentException("StartTime cannot be greater than EndTime");
            }
        }
    }
}