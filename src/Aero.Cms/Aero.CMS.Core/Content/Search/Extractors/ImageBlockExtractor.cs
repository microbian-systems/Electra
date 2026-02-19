using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;

namespace Aero.CMS.Core.Content.Search.Extractors;

public class ImageBlockExtractor : IBlockTextExtractor
{
    public string BlockType => "imageBlock";

    public string? Extract(ContentBlock block)
    {
        if (block is not ImageBlock imageBlock) return null;
        return string.IsNullOrWhiteSpace(imageBlock.Alt) ? null : imageBlock.Alt;
    }
}
