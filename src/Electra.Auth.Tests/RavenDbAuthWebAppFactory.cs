using Electra.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;


namespace Electra.Auth.Tests;

public class RavenDbAuthWebAppFactory : WebApplicationFactory<Program>
{
    private IDocumentStore _documentStore;

    public RavenDbAuthWebAppFactory()
    {
        // Setup RavenDB in-memory for testing
        // We use RavenTestDriver or just a simple DocumentStore with Embedded mode if possible.
        // For simplicity here, we assume RavenDB is available or we mock it.
        // Actually, let's use a real DocumentStore pointed to a test instance if available, 
        // or embedded if the package is available.
        // Since we already have RavenDbTestBase using Raven.TestDriver, 
        // let's try to integrate that.
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
            // We need to provide an IDocumentStore for the UserStore to use.
            // In a real integration test, we'd use Raven.TestDriver.
            // For now, let's register a singleton DocumentStore.
            var store = new DocumentStore
            {
                Urls = new[] { "http://localhost:8080" },
                Database = "ElectraAuthTest"
            }.Initialize();

            services.AddSingleton<IDocumentStore>(store);
            
            // Override the Scoped session to ensure it uses the test store
            services.AddScoped(sp => 
            {
                var s = store.OpenAsyncSession();
                s.Advanced.WaitForIndexesAfterSaveChanges();
                return s;
            });
        });
    }
}
