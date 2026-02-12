using ZauberCMS.Core.Content.Interfaces;
using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Membership.Models;

public class UserPropertyValue : IPropertyValue
{
    public string Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public string Alias { get; set; } = string.Empty;
    public Guid ContentTypePropertyId { get; set; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Value { get; set; } = string.Empty;
    public DateTime? DateUpdated { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }    
    public CmsUser? User { get; set; }
}