using Electra.Web.BlogEngine.Services;
using Electra.Web.Core.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Electra.Web.BlogEngine.Controllers;

[Route("/blog/[action]")]
public class BlogWebController(IBlogService blogService, ILogger<ElectraWebBaseController> log) : ElectraWebBaseController(log)
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("{slug:string}")]
    public async Task<IActionResult> Article(string slug)
    {
        if (string.IsNullOrEmpty("slug"))
            return BadRequest("slug was not provided");
        
        var data = await blogService.GetBlogBySlugAsync(slug);
        return View(data);
    }
}