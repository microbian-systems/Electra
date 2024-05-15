using Electra.Common.Caching.Extensions;

namespace Electra.Common.Web.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddElectraCaching(this IServiceCollection services, IConfiguration config)
    {
        services.AddEasyCaching(config);

        return services;
    }
}