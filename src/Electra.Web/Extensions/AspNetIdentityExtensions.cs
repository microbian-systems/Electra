using Electra.Core.Identity;
using Electra.Models;
using Electra.Models.Entities;
using Electra.Persistence;
using Electra.Persistence.Extensions;
using Electra.Services;
using Microsoft.AspNetCore.Identity;

namespace Electra.Common.Web.Extensions;

public static class AspNetIdentityExtensions
{
    public static IServiceCollection AddAspNetIdentityEx(this IServiceCollection services,
        IConfiguration config, IWebHostEnvironment env)
    {
        services.AddDataLayerPersistence(config, env);
        //services.AddIdentity<ElectraUser, ElectraRole>()
        //     .AddEntityFrameworkStores<ElectraDbContext>()
        //     .AddDefaultTokenProviders();
        services.AddIdentityCore<ElectraUser>()
            .AddRoles<ElectraRole>()
            .AddUserManager<UserManager<ElectraUser>>()
            .AddRoleManager<RoleManager<ElectraRole>>()
            .AddSignInManager<SignInManager<ElectraUser>>()
            .AddEntityFrameworkStores<ElectraDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IElectraIdentityService, ElectraIdentityService>();
        services.AddScoped<IElectraUserProfileService, ElectraUserProfileService>();
        services.AddScoped<IElectraUserProfileRepository, ElectraUserProfileRepository>();

        return services;
    }
}