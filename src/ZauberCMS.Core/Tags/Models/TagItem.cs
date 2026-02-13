using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Tags.Models;

public class TagItem
{
    public string? Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
    public string TagId { get; set; }
    public Tag? Tag { get; set; }
    public string ItemId { get; set; }
}