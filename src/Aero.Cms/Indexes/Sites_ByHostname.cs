using Aero.Cms.Models;
using Raven.Client.Documents.Indexes;

namespace Aero.Cms.Indexes;

public class Sites_ByHostname : AbstractIndexCreationTask<SiteDocument>
{
    public Sites_ByHostname()
    {
        Map = sites => from site in sites
            select new
            {
                site.Hostnames
            };
    }
}