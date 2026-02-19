using Raven.Client.Documents;
using Raven.TestDriver;

namespace Aero.CMS.Tests.Integration.Infrastructure;

public abstract class RavenTestBase : RavenTestDriver
{
    protected IDocumentStore Store => GetDocumentStore();
}
