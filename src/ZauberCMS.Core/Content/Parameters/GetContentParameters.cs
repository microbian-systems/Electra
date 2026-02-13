namespace ZauberCMS.Core.Content.Parameters;

public class GetContentParameters
{
    public bool Cached { get; set; }
    public bool IncludeUnpublishedContent { get; set; }
    public bool IncludeContentRoles { get; set; }
    public string? Id {get; set;}
    public bool IncludeChildren { get; set; }
    public bool IncludeParent { get; set; }
    public bool IncludeUnpublished { get; set; }
    public string ContentTypeAlias { get; set; } = string.Empty;
}