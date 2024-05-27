using EasyCaching.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Common.Caching.Extensions;

public static class EasyCachingExtensions
{
    public static IServiceCollection AddEasyCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEasyCaching(options =>
        {
            //options.UseInMemory(configuration, "default");
            options.UseHybrid(configuration, "hybrid");
        });

        services.AddSingleton<IEasyCachingProvider>(
            sp => sp.GetRequiredService<IEasyCachingProviderFactory>()
                .GetCachingProvider("hybrid"));

        services.AddSingleton(typeof(ICacheService<>), typeof(EasyCachingService<>));

        return services;
    }
}