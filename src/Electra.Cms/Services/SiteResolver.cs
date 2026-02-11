using System;
using System.Threading.Tasks;
using Electra.Cms.Indexes;
using Electra.Cms.Models;
using Microsoft.AspNetCore.Http;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Electra.Cms.Services
{
    public class SiteResolver : ISiteResolver
    {
        private readonly IAsyncDocumentSession _session;

        public SiteResolver(IAsyncDocumentSession session)
        {
            _session = session;
        }

        public async Task<SiteDocument?> ResolveSiteAsync(HttpContext context)
        {
            var host = context.Request.Host.Host;
            
            // Try to find site by hostname
            var site = await _session.Query<SiteDocument, Sites_ByHostname>()
                .FirstOrDefaultAsync(x => x.Hostnames.Contains(host));

            return site;
        }
    }
}
