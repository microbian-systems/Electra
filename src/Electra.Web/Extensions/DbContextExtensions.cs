using System.Reflection;

namespace Electra.Common.Web.Extensions;

public static class DbContextExtensions
{
    public static WebApplicationBuilder AddEfContexts(this WebApplicationBuilder builder)
    {
        builder.AddApiAuthDbContext();
        
        return builder;
    }

    public static WebApplicationBuilder AddApiAuthDbContext(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        builder.AddApiAuthDbContext(config);
        
        return builder;
    }
    
    public static WebApplicationBuilder AddApiAuthDbContext(
        this WebApplicationBuilder builder,
        IConfiguration config)
    {
        var migrationAssembly = typeof(ApiAuthContext)
            .GetTypeInfo()
            .Assembly
            .GetName().Name;

        builder.Services.AddDbContextPool<ApiAuthContext>(o =>
                o.UseSqlite(config.GetConnectionString("DefaultConnection"),
                    x => x.MigrationsHistoryTable("__apiAuthMigrations", "apiauth")
                        .MigrationsAssembly(migrationAssembly)))
            //.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
            ;

        return builder;
    }
}