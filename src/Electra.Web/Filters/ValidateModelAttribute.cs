using Microsoft.AspNetCore.Mvc.Filters;

namespace Electra.Common.Web.Filters;

/// <summary>
/// Introduces Model state auto validation to reduce code duplication
/// </summary>
public class ValidateModelFilterAttribute : ActionFilterAttribute
{
    private ILogger<ValidateModelFilterAttribute> log;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        log = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValidateModelFilterAttribute>>();
        log.LogInformation($"validating model");

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(context.ModelState);
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        log.LogInformation($"model validated");
    }
}