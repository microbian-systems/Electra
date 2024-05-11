using System.Collections.Generic;
using Electra.Validators;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Electra.Common.Web
{
    public static class AppSettingsMiddleware
    {
        public static AppSettings ConfigureAppSettings(this IServiceCollection service, IConfiguration config, IWebHostEnvironment env)
        {
            var appSettingsSection = config.GetSection("AppSettings");
            
            var settings = appSettingsSection.Get<AppSettings>();

            // todo - move AppSettings configuration to middleware module
            // configure strongly typed settings objects
            //log.Information($"retrieving AppSettings section");

            if (string.IsNullOrEmpty(settings.AzureStorage.StorageKey))
                settings.AzureStorage.StorageKey = config.GetValue<string>("AppSettings:AzureStorage:AzureStorageKey");

            if (string.IsNullOrEmpty(settings.ConnStrings.Default))
                settings.ConnStrings.Default = config.GetValue<string>("ConnectionStrings:Default");

            var validator = new AppSettingsValidator();
            var result = validator.Validate(settings);

            //log.Information($"AppSettings validator result: IsValid={result.IsValid}");
            if (!result.IsValid)
            {
                var errors = new List<string>();
                //if (System.Diagnostics.Debugger.IsAttached)
                    //log.Error($"You are running in debug mode/configuration.  make sure you've logged in with the cli command 'az login' (azure command line tools needs to be installed)");
                
                foreach (var error in result.Errors)
                {
                    var err = $"{error.PropertyName}: {error.ErrorMessage}";
                    //log.Error(err);
                    errors.Add(err);
                }

                var message = $"AppSettings config could not be validated {errors.Aggregate((a, b) => $"{a} {Environment.NewLine} {b}")}";
                var ex = new ArgumentException(message);
                //log.Error($"{ex.ToJson()}");
            }
            else
            {
                //log.Information($"AppSettings were successfully loaded");
                //log.Information($"{settings.ToJson()}");
            }

            service.AddOptions();
            service.Configure<AppSettings>(appSettingsSection);
            service.AddSingleton(settings);
            //log.Information($"finished retrieving AppSettings sections");
            return settings;
        }
    }
}