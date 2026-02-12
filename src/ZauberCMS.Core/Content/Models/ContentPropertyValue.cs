using System.Text.Json.Serialization;
using ZauberCMS.Core.Content.Interfaces;
using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Content.Models;

public class ContentPropertyValue : IPropertyValue
{
    public string Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public string ContentId { get; set; }
    [JsonIgnore]
    public Content? Content { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string ContentTypePropertyId { get; set; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Value { get; set; } = string.Empty;
    public DateTime? DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateUpdated { get; set; } = DateTime.UtcNow;
}