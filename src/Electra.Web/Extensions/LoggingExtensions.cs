using Foundatio.Utility;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Electra.Common.Web.Extensions;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddDefaultLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, logConfig) =>
        {
            logConfig
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.WithEnvironmentName()
                .Enrich.WithEnvironmentUserName()
                .Enrich.FromLogContext();
        });

        AddDefaultLogging(builder.Services, builder.Configuration);

        return builder;
    }

    public static IServiceCollection AddDefaultLogging(this IServiceCollection services, IConfiguration config)
    {
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.WithEnvironmentName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.FromLogContext();
        var logger = loggerConfig.CreateLogger();
        services.AddLogging(c =>
        {
            c.ClearProviders();
            c.AddConsole();
            //if (!config.Environment.IsProduction())
                c.AddDebug();

            c.AddSerilog(logger, dispose: true);
            var log = logger.GetLogger();
            log.LogInformation("Logging configured");
        });

        return services;
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