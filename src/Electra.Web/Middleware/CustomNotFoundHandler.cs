using System.Text.RegularExpressions;

namespace Electra.Common.Web.Middleware;

public class CustomNotFoundMiddleware
{
    private readonly RequestDelegate next;
    private readonly Regex apiRegex;

    public CustomNotFoundMiddleware(RequestDelegate next)
    {
        this.next = next;

        // Precompile the regex pattern for matching API routes
        var apiPattern = @"^/api/v\d{1,}/";
        apiRegex = new Regex(apiPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Check if the response status code is 404 (Not Found)
        if (context.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            // Check if the request path matches an API pattern
            if (!IsApiRequest(context.Request.Path))
            {
                // Redirect to a custom error page or handle the not found scenario here
                context.Response.Redirect("/error/404", true);
            }
        }
    }

    private bool IsApiRequest(string requestPath)
    {
        // Check if the request path matches the precompiled API regex pattern
        return apiRegex.IsMatch(requestPath);
    }
}