using System.Text.RegularExpressions;
using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;

namespace Aero.CMS.Core.Content.Search.Extractors;

public class MarkdownBlockExtractor : IBlockTextExtractor
{
    public string BlockType => "markdownBlock";

    public string? Extract(ContentBlock block)
    {
        if (block is not MarkdownBlock mdBlock) return null;
        if (string.IsNullOrWhiteSpace(mdBlock.Markdown)) return null;

        // Simple markdown stripping
        var text = mdBlock.Markdown;
        text = Regex.Replace(text, @"#+", ""); // Headers
        text = Regex.Replace(text, @"\*\*|\*", ""); // Bold/Italic
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1"); // Links
        text = Regex.Replace(text, @"`", ""); // Code
        
        return text.Trim();
    }
}
