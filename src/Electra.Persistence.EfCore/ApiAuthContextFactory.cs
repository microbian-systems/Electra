using Electra.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Electra.Common.Web;

public class ApiAuthContextFactory : IDesignTimeDbContextFactory<ApiAuthContext>
{
    public ApiAuthContext CreateDbContext(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json",true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            //.AddCommandLine()
            .Build();
        var connString = config.GetConnectionString("DefaultConnection");
        var builder = new DbContextOptionsBuilder<ApiAuthContext>();
        builder.UseSqlServer(connString, b
            => b.MigrationsAssembly(typeof(ApiAuthContext).Assembly.FullName));

        return new ApiAuthContext(builder.Options);
    }
}