using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Shared.Interfaces;

namespace Aero.CMS.Core.Content.ContentFinders;

public class DefaultContentFinder : IContentFinder
{
    private readonly IContentRepository _contentRepository;
    private readonly ISystemClock _clock;

    public int Priority => 100;

    public DefaultContentFinder(IContentRepository contentRepository, ISystemClock clock)
    {
        _contentRepository = contentRepository;
        _clock = clock;
    }

    public async Task<ContentDocument?> FindAsync(ContentFinderContext context)
    {
        var content = await _contentRepository.GetBySlugAsync(context.Slug, context.IsPreview);

        if (content == null)
        {
            return null;
        }

        if (context.IsPreview)
        {
            return content;
        }

        var now = _clock.UtcNow;

        if (content.Status == PublishingStatus.Published &&
            content.PublishedAt <= now &&
            (content.ExpiresAt == null || content.ExpiresAt > now))
        {
            return content;
        }

        return null;
    }
}
