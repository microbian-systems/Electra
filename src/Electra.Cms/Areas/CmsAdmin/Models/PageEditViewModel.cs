using Electra.Cms.Blocks;
using Electra.Cms.Models;

namespace Electra.Cms.Areas.CmsAdmin.Models
{
    public class PageEditViewModel
    {
        public PageDocument Page { get; set; }
        public SiteDocument Site { get; set; }
        public IEnumerable<BlockDefinition> AvailableBlocks { get; set; }
    }
}
