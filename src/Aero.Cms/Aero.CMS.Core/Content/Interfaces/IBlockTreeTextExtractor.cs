using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Core.Content.Interfaces;

/// <summary>
/// Defines a service that crawls a content block tree and extracts all searchable text.
/// </summary>
public interface IBlockTreeTextExtractor
{
    /// <summary>
    /// Extracts text from a list of blocks and their children.
    /// </summary>
    /// <param name="blocks">The root blocks to extract from.</param>
    /// <returns>A combined string of all extracted text.</returns>
    string Extract(IEnumerable<ContentBlock> blocks);
}
