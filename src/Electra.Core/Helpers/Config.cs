using System.Configuration;
using System.Text.RegularExpressions;
using ILogger = Serilog.ILogger;

namespace Electra.Core.Helpers
{
    public static class Config
    {
        public static string GetSetting(string key) => ConfigurationManager.AppSettings[key];

        public static string GetConnString(string key) =>
            ConfigurationManager.ConnectionStrings[key]?.ConnectionString;

        internal static string GetStorageConnectionString()
        {
            return GetSetting("blobStorage");
        }

        public static T GetJobCommand<T>(ILogger log = null) //where T : IJobModel
        {
            var config = GetSetting("job");  // todo - check for null on config and do something...
            log?.Information($"config: {config}");
            if (string.IsNullOrEmpty(config))
                return default(T);
            var json = Regex.Unescape(config);
            var model = JsonSerializer.Deserialize<T>(json);

            return model;
        }

        // todo - wire method up to use the Azure KeyVault client
        public static string GetFromAzureVault(string key) => GetSetting(key);
    }
}