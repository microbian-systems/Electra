using Foundatio.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Electra.Common.Web.Extensions;

public static class LoggingExtensions
{
    public static ILogger GetReloadableLogger(this WebApplicationBuilder builder)
    {
        var log = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateBootstrapLogger()
            .GetLogger();

        return log;
    }

    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Services.AddLogging(lb =>
        {
            lb.ClearProviders();
            var log = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
            lb.AddSerilog(log);
        });

        return builder;
    }
}