using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Tags.Models;

public class Tag
{
    public string Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public string TagName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DateUpdated { get; set; } = DateTimeOffset.UtcNow;
    public int SortOrder { get; set; }
    
    public List<TagItem> TagItems { get; set; } = [];
    
    // Not mapped, used for querying
    public int Count { get; set; }
}