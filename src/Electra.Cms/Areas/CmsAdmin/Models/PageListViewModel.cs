using System.Collections.Generic;
using Electra.Cms.Models;

namespace Electra.Cms.Areas.CmsAdmin.Models
{
    public class PageListViewModel
    {
        public List<PageDocument> Pages { get; set; } = new();
        public SiteDocument Site { get; set; }
    }
}
