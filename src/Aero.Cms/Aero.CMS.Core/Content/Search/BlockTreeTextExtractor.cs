using System.Text;
using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Core.Content.Search;

/// <summary>
/// Service that crawls a content block tree and extracts all searchable text.
/// </summary>
public class BlockTreeTextExtractor : IBlockTreeTextExtractor
{
    private readonly Dictionary<string, IBlockTextExtractor> _extractors;

    public BlockTreeTextExtractor(IEnumerable<IBlockTextExtractor> extractors)
    {
        _extractors = extractors.ToDictionary(e => e.BlockType, e => e);
    }

    /// <summary>
    /// Extracts text from a list of blocks and their children using DFS traversal.
    /// </summary>
    /// <param name="blocks">The root blocks to extract from.</param>
    /// <returns>A combined string of all extracted text.</returns>
    public virtual string Extract(IEnumerable<ContentBlock> blocks)
    {
        var sb = new StringBuilder();
        foreach (var block in blocks.OrderBy(x => x.SortOrder))
        {
            Crawl(block, sb);
        }
        return sb.ToString().Trim();
    }

    protected virtual void Crawl(ContentBlock block, StringBuilder sb)
    {
        // 1. Extract from the current block
        if (_extractors.TryGetValue(block.Type, out var extractor))
        {
            var text = extractor.Extract(block);
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (sb.Length > 0) sb.Append("\n\n");
                sb.Append(text);
            }
        }

        // 2. Recursively crawl children
        if (block.Children.Any())
        {
            foreach (var child in block.Children.OrderBy(x => x.SortOrder))
            {
                Crawl(child, sb);
            }
        }
    }
}
