using System.Diagnostics;
using Electra.Common.Web.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Electra.Common.Web.Middleware;

public sealed class RequestResponseActionFilter(ILogger<RequestResponseActionFilter> log)
    : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await ActionExecuting(context);
        var executedContext = await next();
        await ActionExecutedAsync(executedContext);
        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;
        // convert elapsed to hh:mm:ss:ms

        log.LogInformation($"Request took {elapsed} ms", elapsed);
    }

    private async Task ActionExecuting(ActionExecutingContext context)
    {
        var logModel = await context.ToRequestResponseLogModel();
        log.LogInformation($"Request log: {logModel.ToJson()}");
    }
    

    private async Task ActionExecutedAsync(ActionExecutedContext context)
    {
        var logModel = await context.ToRequestResponseLogModel();
        log.LogInformation($"Response log: {logModel.ToJson()}");
    }
}