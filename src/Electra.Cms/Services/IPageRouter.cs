using System.Threading.Tasks;
using Electra.Cms.Models;

namespace Electra.Cms.Services
{
    public interface IPageRouter
    {
        Task<PageDocument?> RouteRequestAsync(string siteId, string path);
    }
}
