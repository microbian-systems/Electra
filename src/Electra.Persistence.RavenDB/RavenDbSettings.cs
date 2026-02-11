namespace Electra.Persistence.RavenDB;

/// <summary>
/// Configuration settings for RavenDB persistence
/// </summary>
public class RavenDbSettings
{
    public const string SectionName = "RavenDb";
    
    /// <summary>
    /// Whether to use embedded RavenDB mode.
    /// Note: For actual embedded mode, RavenDB.Embedded NuGet package is required.
    /// When false, uses standard server connection.
    /// </summary>
    public bool UseEmbedded { get; set; } = false;
    
    /// <summary>
    /// Path for embedded database (used when UseEmbedded is true).
    /// Ignored if UseEmbedded is false.
    /// </summary>
    public string? EmbeddedPath { get; set; }
    
    /// <summary>
    /// RavenDB server URL(s) - can be comma-separated for multiple nodes
    /// </summary>
    public string? Urls { get; set; } = "http://localhost:8080";
    
    /// <summary>
    /// Database name
    /// </summary>
    public string DatabaseName { get; set; } = "ElectraDb";
}
