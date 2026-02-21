using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Core.Content.Services;

public class ContentFinderPipeline(IEnumerable<IContentFinder> finders)
{
    private readonly IReadOnlyList<IContentFinder> _finders = finders.OrderBy(f => f.Priority).ToList();

    public async Task<ContentDocument?> ExecuteAsync(ContentFinderContext context)
    {
        foreach (var finder in _finders)
        {
            var content = await finder.FindAsync(context);
            if (content != null)
            {
                return content;
            }
        }

        return null;
    }
}
