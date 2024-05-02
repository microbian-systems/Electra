using Microsoft.AspNetCore.Mvc;
using ServiceStack;

namespace Electra.Common.Web.Controllers
{
    [Authenticate]
    [ValidateAntiForgeryToken]
    public abstract class AppXWebBaseController : Controller
    {
        protected readonly ILogger log;

        protected AppXWebBaseController(ILogger log)
        {
            this.log = log;
        }
    }
}