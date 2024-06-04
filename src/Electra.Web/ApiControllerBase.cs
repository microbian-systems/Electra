using System.Net.Mime;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Electra.Common.Web;

[Authorize] 
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase(ILogger<ApiControllerBase> log)
    : ControllerBase
{
    protected readonly ILogger<ApiControllerBase> log = log;
    protected Guid GetUserID()
    {
        if (this.User.Claims.FirstOrDefault(x => x.Type == "id") == null)
            throw new Exception("Invalid User");
        if (Guid.TryParse(this.User.Claims.FirstOrDefault(x => x.Type == "id").Value, out Guid custid))
            return custid;
        throw new Exception("Invalid User Format");
    }


    protected virtual InternalErrorResult InternalError(string msg = "")
    {
        log.LogError(msg);
        return new();
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