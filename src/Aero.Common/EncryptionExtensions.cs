
using Aero.Common;
using Aero.Core.Encryption;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aero.Core.Extensions;

public static class EncryptionExtensions
{
    public static IServiceCollection AddEncryptionServices(this IServiceCollection services)
    {
        services.AddTransient<IEncryptor, Aes256Encryptor>(sp =>
        {
            var monitor = sp.GetRequiredService<IOptionsMonitor<AppSettings>>();
            var settings = monitor.CurrentValue;
            var key = settings.AesEncryptionSettings.Key;
            var iv = settings.AesEncryptionSettings.IV;
            var opts = new AesEncryptorOptions(key, iv);
            var encryptor = new Aes256Encryptor(opts);

            return encryptor;
        });
        return services;
    }
}