namespace Aero.CMS.Core.Content.Models.Blocks;

public class MarkdownRendererBlock : ContentBlock
{
    public static string BlockType => "markdownRendererBlock";
    public override string Type => BlockType;

    public string MarkdownContent
    {
        get => Properties.TryGetValue("markdownContent", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["markdownContent"] = value;
    }

    public bool UseTypographyStyles
    {
        get => Properties.TryGetValue("useTypographyStyles", out var value) ? value is bool b && b : true;
        set => Properties["useTypographyStyles"] = value;
    }

    public string MaxWidth
    {
        get => Properties.TryGetValue("maxWidth", out var value) ? value?.ToString() ?? "prose" : "prose";
        set => Properties["maxWidth"] = value;
    }
}