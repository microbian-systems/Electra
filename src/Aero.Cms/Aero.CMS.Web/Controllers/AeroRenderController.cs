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

        // Point directly to the EntryPage dispatcher component
        return new RazorComponentResult<EntryPage>();
    }

    [ActionName("NotFound")]
    public IResult ContentNotFound()
    {
        return Results.NotFound();
    }
}
