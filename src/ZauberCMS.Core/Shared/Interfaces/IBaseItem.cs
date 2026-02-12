namespace ZauberCMS.Core.Shared.Interfaces;

public interface IBaseItem : ITreeItem
{
    string? Url { get; set; }
    int SortOrder { get; set; }
    string? ParentId { get; set; }
    DateTime DateCreated { get; set; }
    DateTime DateUpdated { get; set; }
    List<string> Path { get; set; }
}