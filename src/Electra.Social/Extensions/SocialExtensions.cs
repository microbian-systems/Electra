using Electra.Social.Forem;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Social.Extensions;

public static class SocialExtensions
{
    public static IServiceCollection AddElectraSocials(this IServiceCollection services)
    {
        services.AddForem();
        return services;
    }
}