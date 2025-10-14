// Fixtures/TestWebAppFactory.cs

using Electra.Auth.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

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
            //services.AddElectraAuthentication(env, config);
        });

        return base.CreateHost(builder);
    }
}