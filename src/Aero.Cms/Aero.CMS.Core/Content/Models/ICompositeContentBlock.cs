namespace Aero.CMS.Core.Content.Models;

public interface ICompositeContentBlock
{
    List<ContentBlock> Children { get; }
    string[] AllowedChildTypes { get; }
    bool AllowNestedComposites { get; }
    int? MaxChildren { get; }
}
