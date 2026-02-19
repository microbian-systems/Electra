namespace Aero.CMS.Core.Content.Models.Blocks;

public class RichTextBlock : ContentBlock
{
    public static string BlockType => "richTextBlock";
    public override string Type => BlockType;

    public string Html
    {
        get => Properties.TryGetValue("html", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["html"] = value;
    }
}
