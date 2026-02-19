namespace Aero.CMS.Core.Content.Models.Blocks;

public class RichTextBlock : ContentBlock
{
    public override string Type => "richTextBlock";

    public string Html
    {
        get => Properties.TryGetValue("html", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["html"] = value;
    }
}
