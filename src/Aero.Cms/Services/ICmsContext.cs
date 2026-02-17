using Aero.Cms.Models;

namespace Aero.Cms.Services
{
    public interface ICmsContext
    {
        SiteDocument? Site { get; set; }
        PageDocument? Page { get; set; }
    }

    public class CmsContext : ICmsContext
    {
        public SiteDocument? Site { get; set; }
        public PageDocument? Page { get; set; }
    }
}
