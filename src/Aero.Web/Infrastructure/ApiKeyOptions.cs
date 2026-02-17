namespace Aero.Common.Web.Infrastructure;

/// <summary>
/// Contains options used to generate API Keys by the default factory.
/// </summary>
public record ApiKeyOptions
{
    /// <summary>
    /// Gets or sets a prefix for the keys. For example: "EX-"
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the desider final length for an API key.
    /// </summary>
    public int LengthOfKey { get; set; }

    /// <summary>
    /// Gets or sets whether to generate URL-safe keys. Base64 is used if false.
    /// </summary>
    public bool GenerateUrlSafeKeys { get; set; }
}