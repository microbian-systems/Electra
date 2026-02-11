using Electra.Persistence.Core;
using Electra.Persistence.EfCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddDataLayerPersistence(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host)
    {
        var sp = services.BuildServiceProvider();
        var log = sp.GetRequiredService<ILogger<object>>();

        log.LogInformation($"Adding data later persistence rules");

        // todo - add a configuration to choose the db provider at startup
        // services.AddDbContext<ElectraDbContext>(o =>
        // {
        //     //o.UseInMemoryDatabase("Electra");
        //     o.UseSqlServer(config.GetConnectionString("localdb"));
        // });
        // services.AddDbContext<ElectraDbContext>(o =>
        //     o.UseSqlite(
        //         config.GetConnectionString("sqlite"),
        //         x => x.MigrationsHistoryTable("__ElectraMigrations", "electra")
        //             .MigrationsAssembly(typeof(ElectraDbContext).Assembly.FullName)
        //             .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        log.LogInformation($"configuring generic repositories");
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericEntityFrameworkRepository<>));
        services.AddScoped(typeof(IGenericEntityFrameworkRepository<>), typeof(GenericEntityFrameworkRepository<>));
        services.AddScoped(typeof(IGenericEntityFrameworkRepository<,>), typeof(GenericEntityFrameworkRepository<,>));
        services.AddScoped<IElectraUserProfileRepository, ElectraUserProfileRepository>(); 
        services.AddScoped<IAiUsageLogRepository, AiUsageLogsRepository>();
        services.AddScoped<IApiAuthRepository, ApiAuthRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IElectraUserProfileRepository, ElectraUserProfileRepository>();
        services.AddScoped<IElectraUnitOfWork, ElectraUnitOfWork>();

        return services;
    }

    // public static IServiceCollection AddDataLayerPersistence(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment host)
    //     => AddDataLayerPersistence<ElectraDbContext>(services, configuration, host);
}