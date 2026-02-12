using System;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Common.Web.Middleware;

// todo - verify the XssExtensions are obsoleted by asp.net core antiforgery options
public static class WebSecurityExtensions
{
    public static IServiceCollection ConfigureAntiForgeryOptions(this IServiceCollection services)
    {
        services.Configure<AntiforgeryOptions>(opts =>
        {
            opts.Cookie.Expiration = TimeSpan.FromDays(180);
            opts.Cookie.Name = "Electra.Crsf";
            opts.Cookie.HttpOnly = true;
            opts.Cookie.IsEssential = true;
        });
        return services;
    }

    public static IApplicationBuilder UseServerHardening(this IApplicationBuilder app)
    {
        // todo - add
        return app;
    }

    [Obsolete("Use ConfigureAntiForgeryOptions instead", true)]
    public static void UseXssMiddleware(this IApplicationBuilder app, bool allowFrames = true,
        bool enableContentSecurityPolity = true)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("X-Xss-Protection", "1;mode=block");

            if (allowFrames)
                context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");

            // https://www.syncfusion.com/blogs/post/shield-your-asp-net-mvc-web-applications-with-content-security-policy-csp.aspx
            if (enableContentSecurityPolity)
            {
                // can potentially break blazor admin and potentially swagger/hangfire
                // any new (external) libs (css/jss/fonts, etc) should be added here as a white list
                const string csp =
                    @"upgrade-insecure-requests; default-src 'self'; form-action 'self'; connect-src 'self'; img-src https: data:; font-src 'self' use.fontawesome.com stackpath.bootstrapcdn.com; style-src 'self' stackpath.bootstrapcdn.com use.fontawesome.com 'unsafe-inline'; style-src-elem 'self' stackpath.bootstrapcdn.com use.fontawesome.com 'unsafe-inline'; script-src 'self' stackpath.bootstrapcdn.com code.jquery.com cdn.jsdelivr.net assets.adobedtm.com 'unsafe-inline' 'unsafe-eval'; script-src-elem 'self' stackpath.bootstrapcdn.com code.jquery.com cdn.jsdelivr.net assets.adobedtm.com 'unsafe-inline' 'unsafe-eval';";

                context.Response.Headers.Add("Content-Security-Policy", csp);
            }

            await next();
        });
    }
}