using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aero.Web.Core.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public abstract class AeroWebBaseController(ILogger<AeroWebBaseController> log)
    : Controller
{
    protected readonly ILogger<AeroWebBaseController> log = log;
}