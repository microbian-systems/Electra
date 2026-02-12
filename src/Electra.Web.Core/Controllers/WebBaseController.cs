using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Electra.Web.Core.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public abstract class ElectraWebBaseController(ILogger<ElectraWebBaseController> log)
    : Controller
{
    protected readonly ILogger<ElectraWebBaseController> log = log;
}