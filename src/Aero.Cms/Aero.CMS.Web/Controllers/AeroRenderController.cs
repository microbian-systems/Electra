using Aero.CMS.Core.Content.Models;
using Microsoft.AspNetCore.Mvc;
using Aero.CMS.Web.Components.Pages;

namespace Aero.CMS.Web.Controllers;

public class AeroRenderController : Controller
{
    public IActionResult Index()
    {
        var content = HttpContext.Items["AeroContent"] as ContentDocument;
        if (content == null)
        {
            return NotFound();
        }

        return View(content);
    }

    [ActionName("NotFound")]
    public IActionResult ContentNotFound()
    {
        return NotFound();
    }
}
