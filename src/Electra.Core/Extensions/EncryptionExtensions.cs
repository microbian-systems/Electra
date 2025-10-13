using Electra.Core.Encryption;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Core.Extensions;

public static class EncryptionExtensions
{
    public static IServiceCollection AddEncryptionServices(this IServiceCollection services)
    {
        services.AddScoped<IEncryptor, Aes256Encryptor>();
        return services;
    }
}