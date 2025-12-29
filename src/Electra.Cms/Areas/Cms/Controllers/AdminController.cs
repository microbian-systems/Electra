using Electra.Cms.Areas.CmsAdmin.Models;
using Electra.Cms.Blocks;
using Electra.Cms.Models;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Electra.Cms.Areas.Cms.Controllers
{
    [Area("CmsAdmin")]
    [Route("cms/admin")]
    public class AdminController : Controller
    {
        private readonly IAsyncDocumentSession _session;
        private readonly IBlockRegistry _blockRegistry;

        public AdminController(IAsyncDocumentSession session, IBlockRegistry blockRegistry)
        {
            _session = session;
            _blockRegistry = blockRegistry;
        }

        public async Task<IActionResult> Index()
        {
            var sites = await _session.Query<SiteDocument>().ToListAsync();
            return View(sites);
        }

        [Route("site/{siteId}/pages")]
        public async Task<IActionResult> Pages(string siteId)
        {
            var site = await _session.LoadAsync<SiteDocument>(siteId);
            if (site == null) return NotFound();

            var pages = await _session.Query<PageDocument>()
                .Where(x => x.SiteId == siteId)
                .ToListAsync();

            return View(new PageListViewModel { Site = site, Pages = pages });
        }

        [Route("page/{pageId}/edit")]
        public async Task<IActionResult> Edit(string pageId)
        {
            var page = await _session.LoadAsync<PageDocument>(pageId);
            if (page == null) return NotFound();

            var site = await _session.LoadAsync<SiteDocument>(page.SiteId);
            
            return View(new PageEditViewModel 
            { 
                Page = page, 
                Site = site,
                AvailableBlocks = _blockRegistry.GetAllBlocks()
            });
        }

        [HttpPost]
        [Route("page/{pageId}/save")]
        public async Task<IActionResult> Save(string pageId, [FromForm] PageDocument model)
        {
            var page = await _session.LoadAsync<PageDocument>(pageId);
            if (page == null) return NotFound();

            page.Metadata.Title = model.Metadata.Title;
            page.Metadata.SeoDescription = model.Metadata.SeoDescription;
            page.Slug = model.Slug;
            page.Blocks = model.Blocks;
            page.PublishedState = PagePublishedState.Draft;
            page.Version++;
            page.LastModifiedUtc = System.DateTime.UtcNow;

            await _session.SaveChangesAsync();

            return RedirectToAction("Edit", new { pageId });
        }

        [Route("page/{pageId}/preview")]
        public async Task<IActionResult> Preview(string pageId)
        {
            var page = await _session.LoadAsync<PageDocument>(pageId);
            if (page == null) return NotFound();

            var site = await _session.LoadAsync<SiteDocument>(page.SiteId);
            
            return View(new PageEditViewModel { Page = page, Site = site });
        }
    }
}
