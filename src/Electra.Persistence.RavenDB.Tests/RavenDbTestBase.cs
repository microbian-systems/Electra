using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using Raven.Embedded;

namespace Electra.Persistence.RavenDB.Tests;

public abstract class RavenDbTestBase : IDisposable
{
    private static readonly object _lock = new object();
    private static bool _serverStarted = false;
    
    protected IDocumentStore DocumentStore { get; }

    protected RavenDbTestBase()
    {
        lock (_lock)
        {
            if (!_serverStarted)
            {
                EmbeddedServer.Instance.StartServer(new ServerOptions
                {
                    FrameworkVersion = "8.0.22",
                    DataDirectory = "RavenData"
                });
                _serverStarted = true;
            }
        }

        // Use a unique database name for each test class to avoid collisions
        string dbName = "TestDB_" + Guid.NewGuid().ToString("N");
        DocumentStore = EmbeddedServer.Instance.GetDocumentStore(new DatabaseOptions(dbName));
    }

    public void Dispose()
    {
        // Delete the database after the test is done
        try 
        {
            DocumentStore.Maintenance.Server.Send(new DeleteDatabasesOperation(DocumentStore.Database, hardDelete: true));
        }
        catch 
        {
            // Ignore errors during cleanup
        }
        
        DocumentStore.Dispose();
    }
}
