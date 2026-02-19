namespace Aero.CMS.Core.Content.Models.Blocks;

public class MarkdownBlock : ContentBlock
{
    public override string Type => "markdownBlock";

    public string Markdown
    {
        get => Properties.TryGetValue("markdown", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["markdown"] = value;
    }
}
