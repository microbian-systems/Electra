using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Shared.Interfaces;

namespace ZauberCMS.Core.Shared.Models;

public class TreeStub : ITreeItem
{
    public string Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public string? Url { get; set; }
    public Type? StubType { get; set; }
    public List<TreeBranch> Branches { get; set; } = [];
}