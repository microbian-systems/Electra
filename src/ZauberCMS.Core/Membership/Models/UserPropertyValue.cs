using ZauberCMS.Core.Content.Interfaces;
using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Membership.Models;

public class UserPropertyValue : IPropertyValue
{
    public string Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public string Alias { get; set; } = string.Empty;
    public string ContentTypePropertyId { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTime? DateUpdated { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; }    
    public CmsUser? User { get; set; }
}