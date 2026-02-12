using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace Electra.Common.Web.Extensions;

using Microsoft.AspNetCore.Routing;

public static class EndpointExtensions
{
    // public static IApplicationBuilder Redirect(this IApplicationBuilder app, string from, string to)
    // {
    //     var ep = app.UseEndpoints(ep =>
    //     {
    //         ep.Redirect(from, to);
    //     });
    //
    //     return app;
    // }
    //
    // public static IApplicationBuilder RedirectPermanent(this IApplicationBuilder app, string from, string to)
    // {
    //     var ep = app.UseEndpoints(ep =>
    //     {
    //         ep.RedirectPermanent(from, to);
    //     });
    //
    //     return app;
    // }
    //
    // public static IApplicationBuilder Redirect(this IApplicationBuilder app, params Redirect[] paths)
    // {
    //     var ep = app.UseEndpoints(ep =>
    //     {
    //         foreach (var (from, to, permanent) in paths)
    //         {
    //             ep.MapGet(from, async http => { http.Response.Redirect(to, permanent); });
    //         }
    //     });
    //
    //     return app;
    // }
    
    public static IEndpointRouteBuilder Redirect(this IEndpointRouteBuilder endpoints, string from, string to)
    {
        return Redirect(endpoints, new Redirect(from, to));
    }

    public static IEndpointRouteBuilder RedirectPermanent(this IEndpointRouteBuilder endpoints, string from, string to)
    {
        return Redirect(endpoints,new Redirect(from, to, true));
    }

    public static IEndpointRouteBuilder Redirect(this IEndpointRouteBuilder endpoints, params Redirect[] paths)
    {
        foreach (var (from, to, permanent) in paths)
        {
            endpoints.MapGet(from, http =>
            {
                http.Response.Redirect(to);
                return Task.CompletedTask;
            }) ;
        }

        return endpoints;
    }
}

public record Redirect(string From, string To, bool Permanent = false);