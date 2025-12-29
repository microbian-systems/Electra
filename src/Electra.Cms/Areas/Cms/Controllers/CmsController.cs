using Electra.Cms.Models;
using Electra.Cms.Services;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Session;

namespace Electra.Cms.Areas.Cms.Controllers
{
    [Area("CmsAdmin")]
    public class CmsController : Controller
    {
        private readonly IAsyncDocumentSession _session;
        private readonly IBlockRenderer _blockRenderer;

        public CmsController(IAsyncDocumentSession session, IBlockRenderer blockRenderer)
        {
            _session = session;
            _blockRenderer = blockRenderer;
        }

        [HttpGet("/cms-test")]
        public async Task<IActionResult> Index()
        {
            // Create a dummy page for testing purposes if it doesn't exist
            // In a real scenario, this would be in RavenDB
            var site = new SiteDocument
            {
                Id = "sites/test",
                Name = "Test Site",
                Hostnames = new List<string> { "localhost" }
            };

            var page = new PageDocument
            {
                Id = "pages/cms-test",
                SiteId = site.Id,
                FullUrl = "/cms-test",
                Metadata = new PageMetadata
                {
                    Title = "CMS Test Page",
                    SeoDescription = "A test page for the Electra CMS"
                },
                Blocks = new List<BlockDocument>
                {
                    new BlockDocument
                    {
                        Type = "Hero",
                        Data = new Dictionary<string, object>
                        {
                            { "Title", "Welcome to Electra CMS" },
                            { "Subtitle", "This page is rendered dynamically from blocks." },
                            { "CtaText", "Learn More" },
                            { "CtaUrl", "#" }
                        }
                    },
                    new BlockDocument
                    {
                        Type = "RichText",
                        Data = new Dictionary<string, object>
                        {
                            { "Content", "<h2>Core Features</h2><p>Electra CMS provides a flexible, block-based system for building dynamic web pages.</p>" }
                        }
                    }
                },
                PublishedState = PagePublishedState.Published
            };

            var context = new PageRenderContext(site, page);
            return View("Index", context);
        }
    }
}
