namespace Aero.CMS.Core.Content.Models.Blocks;

public class GridBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "gridBlock";
    public override string Type => BlockType;

    public int Columns
    {
        get => Properties.TryGetValue("columns", out var value) && value != null && int.TryParse(value.ToString(), out var i) ? i : 1;
        set => Properties["columns"] = value;
    }

    public string[] AllowedChildTypes => Array.Empty<string>();
    public bool AllowNestedComposites => false;
    public int? MaxChildren => 12;
}
