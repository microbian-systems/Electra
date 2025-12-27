using Microsoft.Extensions.DependencyInjection;

namespace Electra.Persistence.RavenDB;

public static class RavendDbExtensions
{
    public static IServiceCollection RegisterRavenPersistence(this IServiceCollection services)
    {
        // 1. Register the DocumentStore as a SINGLETON
        // It is expensive to create and should exist once for the lifetime of the app.
        services.AddSingleton<IDocumentStore>(ctx =>
        {
            var store = new DocumentStore
            {
                Urls = new[] { "http://localhost:8080" }, // Your URL
                Database = "ElectraDb" // Your DB Name
            };
            store.Initialize();
            return store;
        });

        // 2. Register the Session as SCOPED
        // This creates a new session for every HTTP request.
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
}