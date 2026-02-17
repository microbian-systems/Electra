using Aero.Cms.Models;
using Raven.Client.Documents.Indexes;

namespace Aero.Cms.Indexes
{
    public class Pages_BySiteAndUrl : AbstractIndexCreationTask<PageDocument>
    {
        public Pages_BySiteAndUrl()
        {
            Map = pages => from page in pages
                           select new
                           {
                               page.SiteId,
                               page.FullUrl,
                               page.PublishedState
                           };
        }
    }
}
