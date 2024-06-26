using System.Reflection;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Electra.Core.Identity;
using Electra.Models;
using Electra.Persistence;

namespace Electra.Common.Web.Extensions;


// todo - revamp iddentity server code (was written for IDS4 initially)
public static class IdentityServerServerExtensions
{
    /// <summary>
    /// Register the Electra generic identity server values
    /// </summary>
    /// <param name="services">service collection</param>
    /// <param name="connString">the connection string that points to the identity enabled database</param>
    /// <returns></returns>
    public static IServiceCollection AddElectraIdentityServerDefaults(this IServiceCollection services, string connString)
        => AddElectraIdentityServerDefaults<ElectraUser>(services, connString);

    /// <summary>
    ///
    /// </summary>
    /// <param name="services">service collection</param>
    /// <param name="connString">the connection string that points to the identity enabled database</param>
    /// <typeparam name="T">the type of Identity User</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddElectraIdentityServerDefaults<T>(this IServiceCollection services, string connString) where T : class
        => AddElectraIdentityServerDefaults<T, ElectraIdentityContext>(services, connString);


    // todo - for AddElectraDefaultIdentityDefaults config method add a param of type IdentityOptions to allow user to configure the identity options
    /// <summary>
    /// Register the Electra generic identity server values
    /// </summary>
    /// <param name="services">service collection</param>
    /// <param name="connString">the connection string that points to the identity enabled database</param>
    /// <typeparam name="T">the type of Identity User</typeparam>
    /// <typeparam name="TContext">the Identity spedcific DbContext</typeparam>
    /// <returns>return service collection</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddElectraIdentityServerDefaults<T, TContext>(this IServiceCollection services, string connString) where T : class where TContext : DbContext, IPersistedGrantDbContext
    {
        if (string.IsNullOrEmpty(connString))
            throw new ArgumentNullException($"connstring cannot be null in {nameof(AddElectraIdentityServerDefaults)} configuraiton method");

        // todo - later once email conf is working switch to the AddDefaultIdentity<T, ElectraRole>()
        services.AddDefaultIdentity<T>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<ElectraRole>()
            .AddEntityFrameworkStores<TContext>()
            ;

        services.AddIdentityServer()
            //.AddAspNetIdentity<T>()
            //.AddInMemoryCaching() // todo - change IDS5 caching to redis
            //.AddInMemoryApiScopes(DuendeConfig.ApiScopes)
            //.AddInMemoryClients(DuendeConfig.Clients) // configuration.GetSection("IdentityServer:Clients")
            //.AddInMemoryIdentityResources(new IdentityResource[]{})
            //.AddInMemoryApiResources(new ApiResource[]{})
            //.AddInMemoryPersistedGrants()
            .AddConfigurationStore(opts =>
            {
                opts.DefaultSchema = "Users";
                var migrationAssembly = typeof(TContext).GetTypeInfo().Assembly.GetName().Name;
                opts.ConfigureDbContext = b => b.UseSqlite(connString,
                    sql => sql.MigrationsAssembly(migrationAssembly));
            })
            .AddOperationalStore(opts =>
            {
                opts.DefaultSchema = "Users";
                var migrationAssembly = typeof(TContext).GetTypeInfo().Assembly.GetName().Name;
                opts.ConfigureDbContext = b => b.UseSqlite(connString,
                    sql => sql.MigrationsAssembly(migrationAssembly));

                // this enables automatic token cleanup. this is optional.
                opts.EnableTokenCleanup = true;
                opts.TokenCleanupInterval = 36000; // interval in seconds (default is 3600)
            })
            //.AddApiAuthorization<T, TContext>()
            ;
        return services;
    }
}