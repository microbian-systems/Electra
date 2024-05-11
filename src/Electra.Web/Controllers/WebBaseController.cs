namespace Electra.Common.Web.Controllers;

[Authorize]
[ValidateAntiForgeryToken]
public abstract class ElectraWebBaseController(ILogger<ElectraWebBaseController> log)
    : Controller
{
    protected readonly ILogger<ElectraWebBaseController> log = log;
}