using Aero.Services;

namespace Aero.Common.Web.Extensions;

public static class AspNetIdentityExtensions
{
    public static IServiceCollection AddAspNetIdentityEx(this IServiceCollection services,
        IConfiguration config, IWebHostEnvironment env)
    {


        services.AddScoped<IAeroIdentityService, AeroIdentityService>();
        services.AddScoped<IAeroUserProfileService, AeroUserProfileService>();

        return services;
    }
}