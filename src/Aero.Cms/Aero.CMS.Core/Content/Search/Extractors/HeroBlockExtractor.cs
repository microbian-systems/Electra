using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;

namespace Aero.CMS.Core.Content.Search.Extractors;

public class HeroBlockExtractor : IBlockTextExtractor
{
    public string BlockType => "heroBlock";

    public string? Extract(ContentBlock block)
    {
        if (block is not HeroBlock hero) return null;
        
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(hero.Heading)) parts.Add(hero.Heading.Trim());
        if (!string.IsNullOrWhiteSpace(hero.Subtext)) parts.Add(hero.Subtext.Trim());

        return parts.Count > 0 ? string.Join("\n", parts) : null;
    }
}
