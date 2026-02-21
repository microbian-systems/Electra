using Aero.CMS.Core.Content.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Aero.CMS.Web.Components.Pages;

namespace Aero.CMS.Web.Controllers;

public class AeroRenderController : Controller
{
    public IResult Index()
    {
        var content = HttpContext.Items["AeroContent"] as ContentDocument;
        if (content == null)
        {
            return Results.NotFound();
        }

        return new RazorComponentResult<PublicPageView>(new { Page = content });
    }

    public IResult NotFound()
    {
        return Results.NotFound();
    }
}
