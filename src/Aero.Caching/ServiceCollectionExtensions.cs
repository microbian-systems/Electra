namespace Aero.Caching;

public static class ServiceCollectionExtensions
{
    // public static IServiceCollection AddRedisCacheClient(this IServiceCollection services, Action<RedisCacheOptions> configure)
    // {
    //     if (services == null)
    //     {
    //         throw new ArgumentNullException(nameof(services));
    //     }
    //
    //     if (configure == null)
    //     {
    //         throw new ArgumentNullException(nameof(configure));
    //     }
    //
    //     services.AddOptions();
    //     services.Configure(configure);
    //     services.Add(ServiceDescriptor.Singleton<IDistributedCache, RedisCache>());
    //
    //     return services;
    // }

    // public static IServiceCollection AddInMemoryCacheClient(this IServiceCollection services)
    //     => services.AddSingleton<IInMemoryCacheClient, InMemoryCacheClient>();

    // public static IServiceCollection AddHybridCacheClient(this IServiceCollection services,
    //     Action<RedisCacheOptions> configure)
    // {
    //     services.AddInMemoryCacheClient();
    //     services.AddRedisCacheClient(configure);
    //     services.AddSingleton<IHybridCacheClient, HybridRedisCacheClient>();
    //     return services;
    // }

}