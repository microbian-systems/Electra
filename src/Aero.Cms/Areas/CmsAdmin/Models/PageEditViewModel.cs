using Aero.Cms.Blocks;
using Aero.Cms.Models;

namespace Aero.Cms.Areas.CmsAdmin.Models
{
    public class PageEditViewModel
    {
        public PageDocument Page { get; set; }
        public SiteDocument Site { get; set; }
        public IEnumerable<BlockDefinition> AvailableBlocks { get; set; }
    }
}
