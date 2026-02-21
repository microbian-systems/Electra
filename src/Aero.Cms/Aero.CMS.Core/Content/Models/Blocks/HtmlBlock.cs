namespace Aero.CMS.Core.Content.Models.Blocks;

public class HtmlBlock : ContentBlock
{
    public static string BlockType => "htmlBlock";
    public override string Type => BlockType;

    public string Html
    {
        get => Properties.GetValueOrDefault("html")?.ToString() ?? string.Empty;
        set => Properties["html"] = value;
    }
}
