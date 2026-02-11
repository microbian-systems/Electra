// Fixtures/TestWebAppFactory.cs

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Electra.Persistence;
using System.Linq;
using Electra.Persistence.EfCore;

namespace Electra.Auth.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Optionally configure additional DI for test overrides
        builder.ConfigureServices((ctx , services)=>
        {
            var env = ctx.HostingEnvironment;
            var config = ctx.Configuration;



            // Ensure generic DbContextOptions is also cleaned if needed, though usually bound to the specific context options
            // But sometimes AddDbContext adds multiple things. 
            
            services.AddDbContext<ElectraDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });
        });

        return base.CreateHost(builder);
    }
}