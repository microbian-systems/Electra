using Electra.Caching.Decorators;
using Electra.Caching.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Web.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddElectraCaching(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(typeof(ICachingRepositoryDecorator<,>), typeof(CachingRepository<,>));
        services.AddScoped(typeof(ICachingRepositoryDecorator<>), typeof(CachingRepository<>));
        return services;
    }
}