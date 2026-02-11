using Electra.Cms.Services;
using Microsoft.AspNetCore.Mvc;

namespace Electra.Cms.Areas.Cms.Controllers
{
    [Area("Cms")]
    public class CmsController : Controller
    {
        private readonly ICmsContext _cmsContext;
        private readonly IBlockRenderer _blockRenderer;

        public CmsController(ICmsContext cmsContext, IBlockRenderer blockRenderer)
        {
            _cmsContext = cmsContext;
            _blockRenderer = blockRenderer;
        }

        public IActionResult Index()
        {
            if (_cmsContext.Page == null || _cmsContext.Site == null)
            {
                return NotFound();
            }

            var context = new PageRenderContext(_cmsContext.Site, _cmsContext.Page);
            return View(context);
        }
    }
}
