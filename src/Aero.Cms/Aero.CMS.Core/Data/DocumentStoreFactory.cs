using Aero.CMS.Core.Settings;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Revisions;

namespace Aero.CMS.Core.Data;

public static class DocumentStoreFactory
{
    public static IDocumentStore Create(RavenDbSettings settings)
    {
        var store = new DocumentStore
        {
            Urls = settings.Urls,
            Database = settings.Database,
            Conventions =
            {
                IdentityPartsSeparator = '/'
            }
        };

        store.Initialize();

        if (settings.EnableRevisions)
        {
            ConfigureRevisions(store, settings.RevisionsToKeep);
        }

        return store;
    }

    private static void ConfigureRevisions(IDocumentStore store, int? revisionsToKeep)
    {
        store.Maintenance.Send(new ConfigureRevisionsOperation(new RevisionsConfiguration
        {
            Default = new RevisionsCollectionConfiguration
            {
                Disabled = false,
                MinimumRevisionsToKeep = revisionsToKeep
            }
        }));
    }
}
