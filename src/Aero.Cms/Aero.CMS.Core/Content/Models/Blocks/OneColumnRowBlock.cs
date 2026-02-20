namespace Aero.CMS.Core.Content.Models.Blocks;

public class OneColumnRowBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "oneColumnRowBlock";
    public override string Type => BlockType;

    public string Padding
    {
        get => Properties.TryGetValue("padding", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["padding"] = value;
    }

    public string Gap
    {
        get => Properties.TryGetValue("gap", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["gap"] = value;
    }

    public string BackgroundColor
    {
        get => Properties.TryGetValue("backgroundColor", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["backgroundColor"] = value;
    }

    public string[] AllowedChildTypes => Array.Empty<string>();
    public bool AllowNestedComposites => true;
    public int? MaxChildren => null;
}