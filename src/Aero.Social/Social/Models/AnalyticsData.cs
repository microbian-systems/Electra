namespace Aero.Social.Models;

/// <summary>
/// Represents analytics data for a provider or post.
/// </summary>
public class AnalyticsData
{
    /// <summary>
    /// Gets or sets the label for this analytics metric (e.g., "Impressions", "Engagement").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data points for this metric.
    /// </summary>
    public List<AnalyticsDataPoint> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the percentage change compared to the previous period.
    /// </summary>
    public double PercentageChange { get; set; }
}

/// <summary>
/// Represents a single analytics data point.
/// </summary>
public class AnalyticsDataPoint
{
    /// <summary>
    /// Gets or sets the total value for this data point.
    /// </summary>
    public string Total { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date for this data point.
    /// </summary>
    public string Date { get; set; } = string.Empty;
}
