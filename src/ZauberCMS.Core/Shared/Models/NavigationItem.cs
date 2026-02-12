using System.ComponentModel.DataAnnotations;
using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Shared.Interfaces;

namespace ZauberCMS.Core.Shared.Models;

public class NavigationItem : ITreeItem
{
    public string Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    
    [Required]
    public string? Name { get; set; }
    
    [Required]
    public string? Url { get; set; }
    
    public List<NavigationItem> Children { get; set; } = [];
    public string? ContentId { get; set; }
    public int SortOrder { get; set; }
    public bool OpenInNewWindow { get; set; }
}