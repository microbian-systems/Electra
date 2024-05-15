using System.Collections.Generic;
using Electra.Common.Extensions;
using Electra.Common.Web.Extensions;
using Electra.Validators;
using MassTransit;

namespace Electra.Common.Web;

public static class AppSettingsExtensions
{
    public static AppSettings ConfigureAppSettings(this IServiceCollection services, IConfiguration config,
        IWebHostEnvironment env)
    {
        var log = LoggingExtensions.GetReloadableLogger(config);

        services.AddOptions();
        var appSettingsSection = config.GetSection("AppSettings");
        services.Configure<AppSettings>(appSettingsSection);
        var settings = appSettingsSection.Get<AppSettings>();
        services.AddSingleton<IOptionsMonitor<AppSettings>, OptionsMonitor<AppSettings>>();

        if (settings is null)
            throw new ConfigurationException("AppSettings section is missing from configuration file");

        log.LogInformation($"retrieving AppSettings section");

        if (string.IsNullOrEmpty(settings.AzureStorage.StorageKey))
            settings.AzureStorage.StorageKey = config.GetValue<string>("AppSettings:AzureStorage:AzureStorageKey");

        var validator = new AppSettingsValidator();
        var result = validator.ValidateAsync(settings)
            .GetAwaiter()
            .GetResult();

        log.LogInformation($"AppSettings validator result: IsValid={result.IsValid}");
        if (!result.IsValid)
        {
            var errors = new List<string>();
            //if (System.Diagnostics.Debugger.IsAttached)
            //log.LogError($"You are running in debug mode/configuration.  make sure you've logged in with the cli command 'az login' (azure command line tools needs to be installed)");

            foreach (var error in result.Errors)
            {
                var err = $"{error.PropertyName}: {error.ErrorMessage}";
                log.LogError(err);
                errors.Add(err);
            }

            var message =
                $"AppSettings config could not be validated {errors.Aggregate((a, b) => $"{a} {Environment.NewLine} {b}")}";
            var ex = new ArgumentException(message);
            log.LogError(ex, $"error validating appsettings config");
        }
        else
        {
            log.LogInformation($"AppSettings were successfully loaded");
            log.LogInformation($"{settings.ToJson()}");
        }

        services.AddSingleton(settings);
        log.LogInformation($"finished retrieving AppSettings sections");
        return settings;
    }
}