namespace Aero.CMS.Core.Content.Models.Blocks;

public class HeroBlock2 : ContentBlock
{
    public static string BlockType => "heroBlock2";
    public override string Type => BlockType;

    public string Title
    {
        get => Properties.TryGetValue("title", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["title"] = value;
    }

    public string Subtitle
    {
        get => Properties.TryGetValue("subtitle", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["subtitle"] = value;
    }

    public string CallToActionText
    {
        get => Properties.TryGetValue("callToActionText", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["callToActionText"] = value;
    }

    public string CallToActionUrl
    {
        get => Properties.TryGetValue("callToActionUrl", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["callToActionUrl"] = value;
    }

    public string BackgroundColor
    {
        get => Properties.TryGetValue("backgroundColor", out var value) ? value?.ToString() ?? "#f8fafc" : "#f8fafc";
        set => Properties["backgroundColor"] = value;
    }

    public string TextColor
    {
        get => Properties.TryGetValue("textColor", out var value) ? value?.ToString() ?? "#1f2937" : "#1f2937";
        set => Properties["textColor"] = value;
    }
}