using Foundatio.Utility;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Electra.Common.Web.Extensions;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddDefaultLogging(this WebApplicationBuilder builder)
    {
        builder.AddSerilogLogging();

        return builder;
    }

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

    public static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration config)
    {
        services.AddLogging(lb =>
        {
            lb.ClearProviders();
            var log = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext()
                .CreateLogger();
            lb.AddSerilog(log);
        });

        return services;
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
            lb.AddSerilog(log, dispose:true);
        });

        return builder;
    }
}