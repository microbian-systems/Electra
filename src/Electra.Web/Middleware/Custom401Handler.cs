using System.Text.RegularExpressions;

namespace Electra.Common.Web.Middleware;

public class Custom401Handler
{
    private readonly RequestDelegate next;
    private readonly Regex apiRegex;

    public Custom401Handler(RequestDelegate next)
    {
        this.next = next;

        // Precompile the regex pattern for matching API routes
        var apiPattern = @"^/api/v\d{1,}/";
        apiRegex = new Regex(apiPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Check if the response status code is 401
        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            // Check if the request path matches an API pattern
            if (!IsApiRequest(context.Request.Path))
            {
                // Redirect to a custom error page or handle the not found scenario here
                context.Response.Redirect("/unauthorized", false);
            }
        }
    }

    private bool IsApiRequest(string requestPath)
    {
        // Check if the request path matches the precompiled API regex pattern
        return apiRegex.IsMatch(requestPath);
    }
}

public static class Custom401Extensions
{
    public static IApplicationBuilder UseCustom401Handler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<Custom401Handler>();
    }
}