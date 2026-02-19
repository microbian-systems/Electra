namespace Aero.CMS.Core.Content.Models;

public class ContentTypeProperty
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public int SortOrder { get; set; }
    public string TabAlias { get; set; } = "content";
    public Dictionary<string, object?> Settings { get; set; } = new();
}
