namespace Aero.CMS.Core.Content.Models.Blocks;

public class TwoColumnRowBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "twoColumnRowBlock";
    public override string Type => BlockType;

    public string Column1Width
    {
        get => Properties.TryGetValue("column1Width", out var value) ? value?.ToString() ?? "50%" : "50%";
        set => Properties["column1Width"] = value;
    }

    public string Column2Width
    {
        get => Properties.TryGetValue("column2Width", out var value) ? value?.ToString() ?? "50%" : "50%";
        set => Properties["column2Width"] = value;
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