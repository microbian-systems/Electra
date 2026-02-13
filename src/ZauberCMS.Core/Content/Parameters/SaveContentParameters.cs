using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Content.Parameters;

public class SaveContentParameters
{
    public Content.Models.Content? Content { get; set; }
    public List<CmsRole> Roles { get; set; } = [];
    public bool UpdateContentRoles { get; set; }
    public bool ExcludePropertyData { get; set; }
    public bool SaveUnpublishedOnly { get; set; }
}