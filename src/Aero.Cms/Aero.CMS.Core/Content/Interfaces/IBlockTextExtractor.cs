using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Core.Content.Interfaces;

/// <summary>
/// Defines a strategy for extracting searchable text from a specific type of content block.
/// </summary>
public interface IBlockTextExtractor
{
    /// <summary>
    /// Gets the block type alias this extractor handles.
    /// </summary>
    string BlockType { get; }

    /// <summary>
    /// Extracts searchable text from the provided block.
    /// </summary>
    /// <param name="block">The content block instance.</param>
    /// <returns>The extracted text, or null if no relevant text exists.</returns>
    string? Extract(ContentBlock block);
}
