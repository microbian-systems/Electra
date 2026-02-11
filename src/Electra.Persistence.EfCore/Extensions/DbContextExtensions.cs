using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Persistence.EfCore.Extensions;

public static class DbContextExtensions
{
    
    public static IServiceCollection AddApiAuthDbContext(this IServiceCollection services, IConfiguration config)
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

        return services;
    }
}