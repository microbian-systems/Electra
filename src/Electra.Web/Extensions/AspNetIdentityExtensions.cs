using Electra.Core.Identity;
using Electra.Models;
using Electra.Persistence;
using Electra.Services;
using Microsoft.AspNetCore.Identity;

namespace Electra.Common.Web.Extensions;

public static class AspNetIdentityExtensions
{
    public static IServiceCollection AddAspNetIdentityEx(this IServiceCollection services,
        IConfiguration config, IWebHostEnvironment env)
    {
        services.AddDataLayerPersistence(config, env);
        services.AddIdentity<ElectraUser, ElectraRole>()
            .AddEntityFrameworkStores<ElectraDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IElectraIdentityService, ElectraIdentityService>();
        services.AddScoped<IElectraUserProfileService, ElectraUserProfileService>();
        services.AddScoped<IElectraUserProfileServiceRepository, ElectraUserProfileServiceRepository>();

        return services;
    }
}