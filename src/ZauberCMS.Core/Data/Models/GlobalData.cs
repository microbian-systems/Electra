using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Data.Models;

public class GlobalData
{
    public string? Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public string? Alias { get; set; }
    
    public string? Data { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;
}