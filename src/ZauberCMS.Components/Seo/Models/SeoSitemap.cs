using ZauberCMS.Core.Content.Models;

namespace ZauberCMS.Components.Seo.Models;

public class SeoSitemap
{
    public string? Name { get; set; }
    public string? Domain { get; set; }
    public string? FileName { get; set; }
    public string RootContentId { get; set; }
    public Content? RootContent { get; set; }
    public List<string> ContentTypeIds { get; set; } = [];
}