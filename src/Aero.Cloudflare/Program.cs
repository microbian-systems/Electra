using Aero.Cloudflare;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder();
var services = builder.Services;
var config = builder.Configuration;
var env = builder.Environment;

services.AddHostedService<DnsUpdaterHostedService>();
services.AddHttpClient();
services.AddHttpContextAccessor();
services.AddHostedService<DnsUpdaterHostedService>();


var log = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CloudFlareDnsUpdater")
    .WriteTo.Console()
    .CreateBootstrapLogger();
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CloudFlareDnsUpdater")
    .WriteTo.Console()
    .CreateLogger();
    
builder.Logging.AddSerilog(logger, dispose: true);

config.AddJsonFile("appsettings.json", true);
config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);
config.AddUserSecrets(typeof(Program).Assembly);
config.AddEnvironmentVariables();

if (args != null)
    config.AddCommandLine(args);

try
{
    log.Information("Starting DNS updater...");
    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    log.Fatal(ex, "DnsUpdater terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}