using Aero.Caching.Decorators;

namespace Aero.Web.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddAeroCaching(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(typeof(ICachingRepositoryDecorator<,>), typeof(CachingRepository<,>));
        services.AddScoped(typeof(ICachingRepositoryDecorator<>), typeof(CachingRepository<>));
        return services;
    }
}