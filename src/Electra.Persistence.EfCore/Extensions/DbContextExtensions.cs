using System.Reflection;
using Aero.Identity;
using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Persistence.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Persistence.EfCore.Extensions;

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
        
        services.AddScoped<IElectraUserProfileRepository, ElectraUserProfileEfCoreRepository>(); 
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericEntityFrameworkRepository<>));
        services.AddScoped(typeof(IGenericEntityFrameworkRepository<>), typeof(GenericEntityFrameworkRepository<>));
        services.AddScoped(typeof(IGenericEntityFrameworkRepository<,>), typeof(GenericEntityFrameworkRepository<,>));
        services.AddScoped<IAiUsageLogRepository, AiUsageLogsRepository>();
        services.AddScoped<IApiAuthRepository, ApiAuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IElectraUnitOfWork, ElectraUnitOfWork>();
        services.AddScoped<IElectraUserProfileRepository, ElectraUserProfileEfCoreRepository>();
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
        
        

        return services;
    }

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