using Electra.Common.Caching.Decorators;
using Electra.Common.Caching.Extensions;

namespace Electra.Common.Web.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddElectraCaching(this IServiceCollection services, IConfiguration config)
    {
        services.AddEasyCaching(config);
        services.AddScoped(typeof(ICachingRepositoryDecorator<,>), typeof(CachingRepository<,>));
        services.AddScoped(typeof(ICachingRepositoryDecorator<>), typeof(CachingRepository<>));
        return services;
    }
}