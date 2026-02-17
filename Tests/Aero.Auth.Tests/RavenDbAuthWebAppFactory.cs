using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.TestDriver;
using Raven.Embedded;

namespace Aero.Auth.Tests;

public class RavenDbAuthWebAppFactory : WebApplicationFactory<Program>
{
    private readonly RavenTestDriverHelper _driverHelper;
    private IDocumentStore _documentStore;

    public RavenDbAuthWebAppFactory()
    {
        _driverHelper = new RavenTestDriverHelper();
        _driverHelper.Configure();
        _documentStore = _driverHelper.GetStore();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:UseRavenDB"] = "true"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IDocumentStore>(_documentStore);
            
            // Override the Scoped session to ensure it uses the test store
            services.AddScoped(sp => 
            {
                var s = _documentStore.OpenAsyncSession();
                // Ensure indexes are up to date for tests
                // s.Advanced.WaitForIndexesAfterSaveChanges(); // Can be expensive, use selectively or globally if needed
                return s;
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _documentStore.Dispose();
            _driverHelper.Dispose();
        }
        base.Dispose(disposing);
    }
}

// Helper class to access RavenTestDriver functionality since we can't inherit multiple classes
public class RavenTestDriverHelper : RavenTestDriver, IDisposable
{
    public void Configure()
    {
        ConfigureServer(new Raven.TestDriver.TestServerOptions
        {
            FrameworkVersion = null,
            Licensing = new ServerOptions.LicensingOptions
            {
                ThrowOnInvalidOrMissingLicense = false
            },
            CommandLineArgs = new List<string>
            {
                "--RunInMemory=true"
            }
        });
    }

    public IDocumentStore GetStore()
    {
        return GetDocumentStore();
    }

    public new void Dispose()
    {
        base.Dispose();
    }
}
