using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Core.Content.Services;

public class ContentFinderPipeline
{
    private readonly IEnumerable<IContentFinder> _finders;
    private static readonly string[] ReservedRoutes = ["/admin", "/not-found", "/_framework", "/_content", "/_blazor"];

    public ContentFinderPipeline(IEnumerable<IContentFinder> finders)
    {
        _finders = finders.OrderBy(f => f.Priority);
    }

    public async Task<ContentDocument?> ExecuteAsync(ContentFinderContext context)
    {
        if (IsReserved(context.Slug))
        {
            return null;
        }

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

    private static bool IsReserved(string slug)
    {
        return ReservedRoutes.Any(r => slug.StartsWith(r, StringComparison.OrdinalIgnoreCase));
    }
}
