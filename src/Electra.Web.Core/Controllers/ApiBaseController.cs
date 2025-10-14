using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Electra.Web.Core.Controllers;

[Authorize]
[ApiController]
[ValidateAntiForgeryToken]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class ElectraApiBaseController(ILogger<ElectraApiBaseController> log) : ApiControllerBase(log)
{
    protected readonly ILogger log = log;
    //private IMediator _mediatorInstance; // todo - why do we need a private "instance" of IMediator ?
    //protected IMediator _mediator => _mediatorInstance ??= HttpContext.RequestServices.GetService<IMediator>();

    protected ActionResult InternalServerError(Exception ex, string message = null)
    {
        log.LogError(ex, $"en error has occured: {string.Join(Environment.NewLine, message, ex.Message)}");
        return new StatusCodeResult(500);
    }
}