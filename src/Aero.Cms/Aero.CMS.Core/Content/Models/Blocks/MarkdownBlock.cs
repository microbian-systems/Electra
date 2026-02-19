namespace Aero.CMS.Core.Content.Models.Blocks;

public class MarkdownBlock : ContentBlock
{
    public static string BlockType => "markdownBlock";
    public override string Type => BlockType;

    public string Markdown
    {
        get => Properties.TryGetValue("markdown", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["markdown"] = value;
    }
}
