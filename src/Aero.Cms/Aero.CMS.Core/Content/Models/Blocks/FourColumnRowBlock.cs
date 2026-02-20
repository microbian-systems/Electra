namespace Aero.CMS.Core.Content.Models.Blocks;

public class FourColumnRowBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "fourColumnRowBlock";
    public override string Type => BlockType;

    public bool EqualColumns
    {
        get => Properties.TryGetValue("equalColumns", out var value) ? value is bool b && b : true;
        set => Properties["equalColumns"] = value;
    }

    public string Gap
    {
        get => Properties.TryGetValue("gap", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["gap"] = value;
    }

    public string Padding
    {
        get => Properties.TryGetValue("padding", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["padding"] = value;
    }

    public string BackgroundColor
    {
        get => Properties.TryGetValue("backgroundColor", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["backgroundColor"] = value;
    }

    public string ResponsiveBreakpoint
    {
        get => Properties.TryGetValue("responsiveBreakpoint", out var value) ? value?.ToString() ?? "md" : "md";
        set => Properties["responsiveBreakpoint"] = value;
    }

    public string[] AllowedChildTypes => Array.Empty<string>();
    public bool AllowNestedComposites => true;
    public int? MaxChildren => null;
}