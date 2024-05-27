using System.Reflection;

namespace Electra.Common.Web.Extensions;

public static class AppSettingsExtensions
{
    public static WebApplicationBuilder AddAppSettings(this WebApplicationBuilder builder)
    {
        var env = builder.Environment;
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets(Assembly.GetExecutingAssembly());

        return builder;
    }
}