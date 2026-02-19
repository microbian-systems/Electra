using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Content.Models;

public class ContentTypeDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "document";
    public bool RequiresApproval { get; set; }
    public bool AllowAtRoot { get; set; }
    
    public string[] AllowedChildContentTypes { get; set; } = Array.Empty<string>();
    public List<ContentTypeProperty> Properties { get; set; } = new();
}
