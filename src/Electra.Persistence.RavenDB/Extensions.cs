using Microsoft.Extensions.DependencyInjection;

namespace Electra.Persistence.RavenDB;

public static class RavendDbExtensions
{
    public static IServiceCollection RegisterRavenPersistence(this IServiceCollection services)
    {
        // Register RavenDB related services here
        // e.g., IDocumentStore, IAsyncDocumentSession, repositories, etc.

        return services;
    }
}