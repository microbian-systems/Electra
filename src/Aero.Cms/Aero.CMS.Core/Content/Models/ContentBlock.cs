namespace Aero.CMS.Core.Content.Models;

public abstract class ContentBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public abstract string Type { get; }
    public int SortOrder { get; set; }
    public Dictionary<string, object?> Properties { get; set; } = new();
    public List<ContentBlock> Children { get; set; } = new();
}
