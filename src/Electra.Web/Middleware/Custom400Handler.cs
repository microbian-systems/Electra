using System.Text.RegularExpressions;

namespace Electra.Common.Web.Middleware;

public class Custom400Handler
{
    private readonly RequestDelegate next;
    private readonly Regex apiRegex;

    public Custom400Handler(RequestDelegate next)
    {
        this.next = next;

        // Precompile the regex pattern for matching API routes
        var apiPattern = @"^/api/v\d{1,}/";
        apiRegex = new Regex(apiPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Check if the response status code is 409
        if (context.Response.StatusCode == StatusCodes.Status400BadRequest)
        {
            // Check if the request path matches an API pattern
            if (!IsApiRequest(context.Request.Path))
            {
                // Redirect to a custom error page or handle the not found scenario here
                context.Response.Redirect("/error", false);
            }
        }
    }

    private bool IsApiRequest(string requestPath)
    {
        // Check if the request path matches the precompiled API regex pattern
        var isMatch = apiRegex.IsMatch(requestPath);
        return isMatch;
    }
}

public static class Custom400Extensions
{
    public static IApplicationBuilder UseCustom400Handler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<Custom400Handler>();
    }
}