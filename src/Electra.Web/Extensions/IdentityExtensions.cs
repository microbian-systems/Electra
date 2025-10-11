using Electra.Persistence;
using Electra.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Electra.Common.Web.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddElectraIdentity<TUser, TRole>(this IServiceCollection services)
        where TUser : IdentityUser<long>, new()
        where TRole : IdentityRole<long>
    {
        services.AddScoped<UserManager<TUser>>();
        services.AddScoped<SignInManager<TUser>>();
        services.AddScoped<SignInManager<TUser>>(); // for some reason the DI container expecting this - probably registered the generic Electra user service - fix later
        services.AddScoped<UserManager<TUser>>();  // for some reason the DI container expecting this - probably registered the generic Electra user service - fix later
        services.AddScoped<UserStore<TUser, TRole, ElectraDbContext, long>>(); // for some reason the DI container expecting this - probably registered the generic Electra user service - fix later
        services.AddScoped<RoleManager<TRole>>();
        services.AddScoped<IRoleService<TRole>, RoleService<TRole>>();

        return services;
    }
}