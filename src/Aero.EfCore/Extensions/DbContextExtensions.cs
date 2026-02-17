using System.Reflection;
using Aero.Core.Identity;
using Aero.EfCore.Data;
using Aero.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aero.EfCore.Extensions;

public static class DbContextExtensions
{
    
    public static IServiceCollection AddApiAuthDbContext(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        var migrationAssembly = typeof(ApiAuthContext)
            .GetTypeInfo()
            .Assembly
            .GetName().Name;

        services.AddDbContextPool<ApiAuthContext>(o =>
                o.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                    x => x.MigrationsHistoryTable("__apiAuthMigrations", "apiauth")
                        .MigrationsAssembly(migrationAssembly)))
            //.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
            ;
        

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericEntityFrameworkRepository<>));
        services.AddScoped(typeof(IGenericEntityFrameworkRepository<>), typeof(GenericEntityFrameworkRepository<>));
        services.AddScoped(typeof(IGenericEntityFrameworkRepository<,>), typeof(GenericEntityFrameworkRepository<,>));
        services.AddScoped<IAiUsageLogRepository, AiUsageLogsRepository>();
        services.AddScoped<IApiAuthRepository, ApiAuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        //services.AddDataLayerPersistence(config, env);
        //services.AddIdentity<AeroUser, AeroRole>()
        //     .AddEntityFrameworkStores<AeroDbContext>()
        //     .AddDefaultTokenProviders();
        services.AddIdentityCore<AeroUser>()
            .AddRoles<AeroRole>()
            .AddUserManager<UserManager<AeroUser>>()
            .AddRoleManager<RoleManager<AeroRole>>()
            .AddSignInManager<SignInManager<AeroUser>>()
            .AddEntityFrameworkStores<AeroDbContext>()
            .AddDefaultTokenProviders();
        
        

        return services;
    }

    // todo - this should exist in the Aero.Identityh project
    public static IServiceCollection AddAeroIdentity<TUser, TRole>(this IServiceCollection services)
        where TUser : IdentityUser<long>, new()
        where TRole : IdentityRole<long>
    {
        services.AddScoped<UserManager<TUser>>();
        services.AddScoped<SignInManager<TUser>>();
        services.AddScoped<SignInManager<TUser>>(); // for some reason the DI container expecting this - probably registered the generic Aero user service - fix later
        services.AddScoped<UserManager<TUser>>();  // for some reason the DI container expecting this - probably registered the generic Aero user service - fix later
        services.AddScoped<UserStore<TUser, TRole, AeroDbContext, long>>(); // for some reason the DI container expecting this - probably registered the generic Aero user service - fix later
        services.AddScoped<RoleManager<TRole>>();

        return services;
    }
}