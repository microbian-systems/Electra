using Microsoft.Extensions.DependencyInjection;

namespace Electra.Caching.Extensions;

public static class CacheExtensions
{
    public static IServiceCollection AddElectraCaching(this IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = "localhost:6379";
            });
        
        services.AddScoped<ICacheService, FusionCacheClient>();
        services.AddScoped<IFusionCacheClient, FusionCacheClient>();
        
        return services;
    }
}