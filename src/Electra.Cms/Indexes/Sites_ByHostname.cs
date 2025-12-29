using System.Linq;
using Electra.Cms.Models;
using Raven.Client.Documents.Indexes;

namespace Electra.Cms.Indexes
{
    public class Sites_ByHostname : AbstractIndexCreationTask<SiteDocument>
    {
        public Sites_ByHostname()
        {
            Map = sites => from site in sites
                           from hostname in site.Hostnames
                           select new
                           {
                               Hostname = hostname
                           };
        }
    }
}
