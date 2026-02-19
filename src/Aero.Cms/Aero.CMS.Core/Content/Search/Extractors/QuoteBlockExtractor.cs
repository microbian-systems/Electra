using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;

namespace Aero.CMS.Core.Content.Search.Extractors;

public class QuoteBlockExtractor : IBlockTextExtractor
{
    public string BlockType => "quoteBlock";

    public string? Extract(ContentBlock block)
    {
        if (block is not QuoteBlock quoteBlock) return null;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(quoteBlock.Quote)) parts.Add(quoteBlock.Quote.Trim());
        if (!string.IsNullOrWhiteSpace(quoteBlock.Attribution)) parts.Add(quoteBlock.Attribution.Trim());

        return parts.Count > 0 ? string.Join("\n", parts) : null;
    }
}
