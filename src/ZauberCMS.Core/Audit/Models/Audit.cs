using System.ComponentModel.DataAnnotations;
using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Shared.Interfaces;

namespace ZauberCMS.Core.Audit.Models;

public class Audit : ITreeItem
{
    [MaxLength(25)]
    public string Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public string? Name
    {
        get => Description;
        set => Description = value;
    }
}