using Electra.Cms.Models;

namespace Electra.Cms.Services
{
    public class PageRenderContext
    {
        public SiteDocument Site { get; }
        public PageDocument Page { get; }
        public bool IsEditMode { get; }

        public PageRenderContext(SiteDocument site, PageDocument page, bool isEditMode = false)
        {
            Site = site;
            Page = page;
            IsEditMode = isEditMode;
        }
    }
}
