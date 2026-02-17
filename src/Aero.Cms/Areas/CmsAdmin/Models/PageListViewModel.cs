using Aero.Cms.Models;

namespace Aero.Cms.Areas.CmsAdmin.Models
{
    public class PageListViewModel
    {
        public List<PageDocument> Pages { get; set; } = new();
        public SiteDocument Site { get; set; }
    }
}
