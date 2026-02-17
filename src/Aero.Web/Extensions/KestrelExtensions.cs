using System.Security.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Aero.Common.Web.Extensions;

public static class KestrelExtensions
{
    public static WebApplicationBuilder ConfigureKestrel(this WebApplicationBuilder builder, int port = 80)
    {
        // https://learn.microsoft.com/en-us/visualstudio/containers/container-build?view=vs-2022
        // https://learn.microsoft.com/en-us/aspnet/core/security/docker-https?view=aspnetcore-7.0
        // var cert = new SelfSignerTool()
        //     .Generate("", subjectName: "inventory.microservice");

        builder.WebHost.UseKestrel(opts =>
        { 
            opts.AddServerHeader = false;
            opts.ListenAnyIP(port, o =>
            {
                o.UseHttps();
                o.Protocols = HttpProtocols.Http1AndHttp2AndHttp3; // todo - try adn only use Http3
            });
            opts.ConfigureHttpsDefaults(opts =>
            {
                //opts.ServerCertificate = cert;
                opts.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            });
        });

        builder.WebHost.UseIISIntegration();
        
        return builder;
    }
}