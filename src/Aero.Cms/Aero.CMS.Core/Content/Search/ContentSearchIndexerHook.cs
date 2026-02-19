using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Shared.Interfaces;

namespace Aero.CMS.Core.Content.Search;

/// <summary>
/// Hook that populates search metadata and extracted text before saving a content document.
/// </summary>
public class ContentSearchIndexerHook : IBeforeSaveHook<ContentDocument>
{
    private readonly IBlockTreeTextExtractor _treeExtractor;
    private readonly ISystemClock _clock;

    public int Priority => 10;

    public ContentSearchIndexerHook(IBlockTreeTextExtractor treeExtractor, ISystemClock clock)
    {
        _treeExtractor = treeExtractor;
        _clock = clock;
    }

    public Task ExecuteAsync(ContentDocument entity)
    {
        // 1. Extract search text from blocks
        entity.SearchText = _treeExtractor.Extract(entity.Blocks);

        // 2. Populate metadata
        if (entity.SearchMetadata == null)
        {
            entity.SearchMetadata = new SearchMetadata();
        }

        // Title logic: "pageTitle" property > Document Name
        if (entity.Properties.TryGetValue("pageTitle", out var titleObj) && titleObj is string title && !string.IsNullOrWhiteSpace(title))
        {
            entity.SearchMetadata.Title = title;
        }
        else
        {
            entity.SearchMetadata.Title = entity.Name;
        }

        // Last indexed timestamp
        entity.SearchMetadata.LastIndexed = _clock.UtcNow;

        return Task.CompletedTask;
    }
}
