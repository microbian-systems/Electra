using System.Net.Mime;

namespace Aero.Common.Web.Exceptions;

public static class ExceptionMiddlewareExtensions
{
    public static void ConfigureExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
    
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "an unhandled exception occurred: {msg}", ex.Message);
            var path = httpContext.Request.Path.Value;
            if(!string.IsNullOrEmpty(path) && path.Contains("/api"))
                await HandleExceptionAsync(httpContext, ex);
            else
                httpContext.Response.Redirect("/error");
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // todo - check if the /api/ path exists and if so return json - else - redirect to page
        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        return context.Response.WriteAsync(new //ErrorDetails()
        {
            StatusCode = context.Response.StatusCode,
            Message = $"an error occurred: {exception.Message}"
        }.ToString() ?? "an error occurred"); // todo - replace ToString() with ToJson()
    }
}