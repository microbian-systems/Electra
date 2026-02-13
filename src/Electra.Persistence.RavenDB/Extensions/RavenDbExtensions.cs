using System.Linq;
using System.Reflection;
using Electra.Persistence.RavenDB.Indexes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using Raven.Embedded;

namespace Electra.Persistence.RavenDB.Extensions;

public static class RavenDbExtensions
{
    public static IServiceCollection AddRavenPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Load RavenDB settings from configuration
        var ravenDbSettings = configuration.GetSection(RavenDbSettings.SectionName).Get<RavenDbSettings>() 
            ?? new RavenDbSettings();

        // 1. Register the DocumentStore as a SINGLETON
        // It is expensive to create and should exist once for the lifetime of the app.
        services.AddSingleton<IDocumentStore>(ctx =>
        {
            IDocumentStore store;

            if (ravenDbSettings.UseEmbedded)
            {
                // For embedded RavenDB, you need to add RavenDB.Embedded NuGet package
                // and uncomment the code below:

                var embeddedOptions = new ServerOptions();

                if (!string.IsNullOrWhiteSpace(ravenDbSettings.EmbeddedPath))
                {
                    //embeddedOptions.DataDirectory = ravenDbSettings.EmbeddedPath;
                }

                EmbeddedServer.Instance.StartServer(embeddedOptions);

                store = EmbeddedServer.Instance.GetDocumentStore(
                    new DatabaseOptions(ravenDbSettings.DatabaseName));
            }
            else
            {
                // Use server-based RavenDB
                var urls = ravenDbSettings.Urls?
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .ToArray() ?? ["http://localhost:8080"];

                store = new DocumentStore
                {
                    Urls = urls,
                    Database = ravenDbSettings.DatabaseName
                };
            }

            store.Initialize();
            return store;
        });

        // 2. Register the Session as SCOPED
        // This creates a new session for every HTTP request.
        services.AddScoped<IDocumentSession>(sp =>
        {
            var store = sp.GetRequiredService<IDocumentStore>();
            return store.OpenSession();
        });
        services.AddScoped<IAsyncDocumentSession>(ctx =>
        {
            var store = ctx.GetRequiredService<IDocumentStore>();
            return store.OpenAsyncSession();
        });

        // 3. Register your Unit of Work as SCOPED
        // It depends on the Scoped session above.
        services.AddScoped<IRavenDbUnitOfWork, RavenDbUnitOfWork>();
        services.AddScoped<IElectraUserRepository>(ctx => 
            ctx.GetRequiredService<IRavenDbUnitOfWork>().Users);

        return services;
    }

    // todo - ensure to call RegisterRavenIndexes()
    public static WebApplication RegisterRavenIndexes(this WebApplication app)
        => RegisterRavenIndexes(app, typeof(Users_ByRoleName).Assembly);
    
    public static WebApplication RegisterRavenIndexes(this WebApplication app, params Assembly[] assemblies)
    {
        var sp = app.Services;
        var store = sp.GetRequiredService<IDocumentStore>();
        
        foreach (var ass in assemblies)
            IndexCreation.CreateIndexes(ass, store);
        
        return app;
    }
}
