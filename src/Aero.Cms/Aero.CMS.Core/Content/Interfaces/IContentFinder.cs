using Aero.CMS.Core.Content.Models;

namespace Aero.CMS.Core.Content.Interfaces;

public interface IContentFinder
{
    int Priority { get; }
    Task<ContentDocument?> FindAsync(ContentFinderContext context);
}
