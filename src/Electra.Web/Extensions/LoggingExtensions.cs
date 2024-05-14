using Foundatio.Utility;
using Microsoft.Extensions.Configuration;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Electra.Common.Web.Extensions;

public static class LoggingExtensions
{
    public static ILogger GetReloadableLogger(this IServiceCollection builder, IConfiguration config)
        => GetReloadableLogger(config);

    public static ILogger GetReloadableLogger(this WebApplicationBuilder builder)
        => GetReloadableLogger(builder.Configuration);

    public static ILogger GetReloadableLogger(IConfiguration config)
    {
        var log = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
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