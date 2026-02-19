namespace Aero.CMS.Core.Settings;

public class GlobalSettings
{
    public RavenDbSettings RavenDb { get; set; } = new();
}

public class RavenDbSettings
{
    public string[] Urls { get; set; } = Array.Empty<string>();
    public string Database { get; set; } = string.Empty;
    public bool EnableRevisions { get; set; }
    public int? RevisionsToKeep { get; set; }
}
