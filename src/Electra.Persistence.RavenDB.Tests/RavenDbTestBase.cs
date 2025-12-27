using Raven.Client.Documents;
using Raven.Embedded;

namespace Electra.Persistence.RavenDB.Tests;

public abstract class RavenDbTestBase : IDisposable
{
    protected IDocumentStore DocumentStore { get; }

    protected RavenDbTestBase()
    {
        EmbeddedServer.Instance.StartServer(new ServerOptions
        {
            FrameworkVersion = "8.0.22",
            DataDirectory = "RavenData"
        });

        DocumentStore = EmbeddedServer.Instance.GetDocumentStore(new DatabaseOptions("TestDB")
        {
            // In-memory mode
        });
        
        // Wait for server to be ready and database to be created
        // (Embedded usually handles this synchronously on GetDocumentStore)
    }

    public void Dispose()
    {
        DocumentStore.Dispose();
        // Note: EmbeddedServer.Instance.Dispose() could be called, 
        // but typically we keep it running for the duration of the test run.
    }
}
