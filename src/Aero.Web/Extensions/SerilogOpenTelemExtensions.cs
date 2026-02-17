using Honeycomb.OpenTelemetry;
using Honeycomb.Serilog.Sink;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Exceptions;
using Serilog.Sinks.OpenTelemetry;
using ThrowGuard;

namespace Aero.Common.Web.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder ConfigureSerilog(this WebApplicationBuilder builder,
        IConfiguration config, string serviceName, string serviceVersion="1.0.0")
    {
        if(string.IsNullOrEmpty(serviceName))
            Throw.BadArg(nameof(serviceName), "serviceName cannot be null or empty");
        var log = GetLogger(config, serviceName, serviceVersion, builder.Environment.EnvironmentName);
        builder.Host.UseSerilog(log, dispose: true);
        return builder;
    }

    public static IHostBuilder ConfigureSerilog(this HostBuilder builder, IConfiguration configuration, string? serviceName=null)
    {
        serviceName??= "Aero.Web";
        var serviceVersion = typeof(SerilogExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        // todo - get the environment from the IHostEnvironment interface
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        if (string.IsNullOrEmpty(environment))
            Throw.BadArg(nameof(environment), "environment cannot be null or empty");
        
        var log = GetLogger(configuration, serviceName, serviceVersion, environment);

        return builder.UseSerilog(log);
    }

    public static Serilog.ILogger GetBootstrapLogger(IConfiguration configuration, string serviceName, string serviceVersion, string environment)
    {
        var opts = configuration.GetHoneycombOptions();
        opts ??= new HoneycombOptions();
        opts.ApiKey ??= configuration["HoneycombApiKey"];
        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            //.Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithExceptionDetails()
            .Enrich.WithSpan()
            //.Enrich.WithProperty("Application", serviceName)
            //.Enrich.WithProperty("Version", serviceVersion)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File($"logs/log-{DateTime.Now:yyyyMMdd}.log")
            .WriteTo.HoneycombSink(opts.ApiKey, opts.ApiKey)
            .WriteTo.OpenTelemetry(options =>
            {
                // Configure the OpenTelemetry protocol
                options.Protocol = OtlpProtocol.Grpc;
            
                // Set endpoint if configured
                if (!string.IsNullOrEmpty(configuration["OpenTelemetry:Endpoint"]))
                {
                    options.Endpoint = configuration["OpenTelemetry:Endpoint"];
                }
            
                // Add resource attributes
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                    ["service.version"] = serviceVersion,
                    ["deployment.environment"] = environment,
                    ["host.name"] = Environment.MachineName
                };
            })
            .CreateBootstrapLogger();
        
        return logger;
    }
    
    private static Serilog.ILogger GetLogger(IConfiguration configuration, string serviceName, string serviceVersion, string environment) 
    {
        var opts = configuration.GetHoneycombOptions();
        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            //.Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithExceptionDetails()
            .Enrich.WithSpan()
            .Enrich.WithProperty("Application", serviceName)
            .Enrich.WithProperty("Version", serviceVersion)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File($"logs/log-{DateTime.Now:yyyyMMdd}.log")
            .WriteTo.HoneycombSink("", "")
            .WriteTo.OpenTelemetry(options =>
            {
                // Configure the OpenTelemetry protocol
                options.Protocol = OtlpProtocol.Grpc;
            
                // Set endpoint if configured
                if (!string.IsNullOrEmpty(configuration["OpenTelemetry:Endpoint"]))
                {
                    options.Endpoint = configuration["OpenTelemetry:Endpoint"];
                }
            
                // Add resource attributes
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                    ["service.version"] = serviceVersion,
                    ["deployment.environment"] = environment,
                    ["host.name"] = Environment.MachineName
                };
            })
            .CreateLogger();
        
        return logger;
    }
}