using System.Text.RegularExpressions;
using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;

namespace Aero.CMS.Core.Content.Search.Extractors;

public class RichTextBlockExtractor : IBlockTextExtractor
{
    public string BlockType => "richTextBlock";

    public string? Extract(ContentBlock block)
    {
        if (block is not RichTextBlock richText) return null;
        if (string.IsNullOrWhiteSpace(richText.Html)) return null;

        // Simple HTML stripping - for production use a robust library like HtmlAgilityPack
        var decoded = System.Net.WebUtility.HtmlDecode(richText.Html);
        var stripped = Regex.Replace(decoded, "<[^>]+>", " ").Trim();
        return Regex.Replace(stripped, @"\s+", " ");
    }
}
