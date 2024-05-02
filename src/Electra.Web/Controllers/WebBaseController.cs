using Microsoft.AspNetCore.Mvc;
using ServiceStack;

namespace Electra.Common.Web.Controllers
{
    [Authenticate]
    [ValidateAntiForgeryToken]
    public abstract class ElectraWebBaseController : Controller
    {
        protected readonly ILogger log;

        protected ElectraWebBaseController(ILogger log)
        {
            this.log = log;
        }
    }
}