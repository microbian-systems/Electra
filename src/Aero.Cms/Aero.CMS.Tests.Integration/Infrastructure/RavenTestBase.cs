using Raven.Client.Documents;
using Raven.TestDriver;
using Raven.Embedded;

namespace Aero.CMS.Tests.Integration.Infrastructure;

public abstract class RavenTestBase : RavenTestDriver
{
    static RavenTestBase()
    {
        ConfigureServer(new TestServerOptions
        {
            Licensing = new ServerOptions.LicensingOptions
            {
                ThrowOnInvalidOrMissingLicense = false
            }
        });
    }

    protected IDocumentStore Store => GetDocumentStore();
}
