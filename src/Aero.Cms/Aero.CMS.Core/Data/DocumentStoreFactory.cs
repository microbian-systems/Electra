using Aero.CMS.Core.Settings;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Revisions;
using System.Security.Cryptography.X509Certificates;

namespace Aero.CMS.Core.Data;

public static class DocumentStoreFactory
{
    public static Func<RavenDbSettings, IDocumentStore> Factory { get; set; } = CreateInternal;

    public static IDocumentStore Create(RavenDbSettings settings)
    {
        return Factory(settings);
    }

    internal static IDocumentStore CreateInternal(RavenDbSettings settings)
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

        if (!string.IsNullOrEmpty(settings.CertificatePath) && File.Exists(settings.CertificatePath))
        {
            store.Certificate = new X509Certificate2(
                settings.CertificatePath,
                settings.CertificatePassword);
        }

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
