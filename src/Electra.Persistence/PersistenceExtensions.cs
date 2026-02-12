using Electra.Models.Entities;
using Electra.Persistence.Core;

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



        return services;
    }

    // public static IServiceCollection AddDataLayerPersistence(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment host)
    //     => AddDataLayerPersistence<ElectraDbContext>(services, configuration, host);
}