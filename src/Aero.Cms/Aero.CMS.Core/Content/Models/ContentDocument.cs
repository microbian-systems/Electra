using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Content.Models;

public class ContentDocument : AuditableDocument
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ContentTypeAlias { get; set; } = string.Empty;
    public PublishingStatus Status { get; set; } = PublishingStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public string LanguageCode { get; set; } = "en";
    
    public Dictionary<string, object?> Properties { get; set; } = new();
    public List<ContentBlock> Blocks { get; set; } = new();
    
    public string SearchText { get; set; } = string.Empty;
    public SearchMetadata SearchMetadata { get; set; } = new();
}
