using Aero.CMS.Core.Content.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aero.CMS.Web.Controllers;

public class AeroRenderController : Controller
{
    private readonly ILogger<AeroRenderController> _logger;

    public AeroRenderController(ILogger<AeroRenderController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.LogDebug("AeroRenderController.Index: Entering action.");

        var content = HttpContext.Items["AeroContent"] as ContentDocument;
        if (content == null)
        {
            _logger.LogWarning("AeroRenderController.Index: AeroContent not found in HttpContext.Items.");
            return NotFound();
        }

        _logger.LogInformation("AeroRenderController.Index: Rendering content for slug: {Slug}, Name: {Name}", content.Slug, content.Name);

        // Explicitly return the View with the content model
        return View("Index", content);
    }

    [ActionName("NotFound")]
    public IActionResult ContentNotFound()
    {
        _logger.LogInformation("AeroRenderController.NotFound: Content not found route triggered.");
        return NotFound();
    }
}
