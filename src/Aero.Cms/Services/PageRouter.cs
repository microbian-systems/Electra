using Aero.Cms.Indexes;
using Aero.Cms.Models;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Aero.Cms.Services
{
    public class PageRouter : IPageRouter
    {
        private readonly IAsyncDocumentSession _session;

        public PageRouter(IAsyncDocumentSession session)
        {
            _session = session;
        }

        public async Task<PageDocument?> RouteRequestAsync(string siteId, string path)
        {
            // Ensure path starts with /
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            // Remove trailing slash if it exists and path is not just /
            if (path.Length > 1 && path.EndsWith("/"))
            {
                path = path.TrimEnd('/');
            }

            var page = await _session.Query<PageDocument, Pages_BySiteAndUrl>()
                .FirstOrDefaultAsync(x => x.SiteId == siteId && x.FullUrl == path && x.PublishedState == PagePublishedState.Published);

            return page;
        }
    }
}
