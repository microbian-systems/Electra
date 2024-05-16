using Electra.Common.Web.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Electra.Common.Web.Middleware;

public sealed class RequestResponseLogFilter(ILogger<RequestResponseLogFilter> log)
    : ActionFilterAttribute, IAsyncActionFilter
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await ActionExecuting(context);
        var executedContext = await next();
        await ActionExecutedAsync(executedContext);
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