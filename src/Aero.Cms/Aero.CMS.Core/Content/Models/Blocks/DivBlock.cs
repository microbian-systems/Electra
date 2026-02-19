namespace Aero.CMS.Core.Content.Models.Blocks;

public class DivBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "divBlock";
    public override string Type => BlockType;

    public string CssClass
    {
        get => Properties.TryGetValue("cssClass", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["cssClass"] = value;
    }

    public string[] AllowedChildTypes => Array.Empty<string>();
    public bool AllowNestedComposites => true;
    public int? MaxChildren => null;
}
