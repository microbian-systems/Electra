using Aero.Social.Forem;
using Microsoft.Extensions.DependencyInjection;

namespace Aero.Social.Extensions;

public static class SocialExtensions
{
    public static IServiceCollection AddAeroSocials(this IServiceCollection services)
    {
        services.AddForem();
        return services;
    }
}