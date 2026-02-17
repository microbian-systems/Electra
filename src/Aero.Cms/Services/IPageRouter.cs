using Aero.Cms.Models;

namespace Aero.Cms.Services
{
    public interface IPageRouter
    {
        Task<PageDocument?> RouteRequestAsync(string siteId, string path);
    }
}
