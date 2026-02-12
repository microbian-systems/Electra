using System;
using System.Linq;
using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Electra.Web.Core.Controllers;

[Authorize] 
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[EnableRateLimiting("api")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status202Accepted)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status304NotModified)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class ElectraApiBaseController(ILogger<ElectraApiBaseController> log)
    : ControllerBase
{
    protected readonly ILogger<ElectraApiBaseController> log = log;
    protected Guid GetUserID()
    {
        if (this.User.Claims.FirstOrDefault(x => x.Type == "id") == null)
            throw new Exception("Invalid User");
        if (Guid.TryParse(this.User.Claims.FirstOrDefault(x => x.Type == "id").Value, out Guid custid))
            return custid;
        throw new Exception("Invalid User Format");
    }


    protected virtual InternalErrorResult InternalError() => new();

    protected ActionResult InternalServerError(Exception ex, string message = null)
    {
        log.LogError(ex, $"en error has occured: {string.Join(Environment.NewLine, message, ex.Message)}");
        return new StatusCodeResult(500);
    }
}

/// <summary>
/// Represents an <see cref="InternalErrorResult"/> that when
/// executed will produce an error (500) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class InternalErrorResult : StatusCodeResult
{
    private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;

    /// <summary>
    /// Creates a new <see cref="InternalErrorResult"/> instance.
    /// </summary>
    public InternalErrorResult() : base(DefaultStatusCode)
    {
    }
}