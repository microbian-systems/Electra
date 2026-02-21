namespace Aero.CMS.Core.Content.Models.Blocks;

public class ColumnBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "columnBlock";
    public override string Type => BlockType;

    public int ColIndex
    {
        get => int.TryParse(
                   Properties.GetValueOrDefault("colIndex")?.ToString(),
                   out var c) ? c : 0;
        set => Properties["colIndex"] = value.ToString();
    }

    public string[]? AllowedChildTypes => null;
    public bool AllowNestedComposites => false;
    public int? MaxChildren => -1;
}
