using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Shared.Models;

public class Tab
{
    public string? Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Alias => Name.ToLowerInvariant().Replace(" ", "-");
    public int SortOrder { get; set; }
    public bool IsSystemTab { get; set; }
    public bool IsCompositionTab { get; set; }
}