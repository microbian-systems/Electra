using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using ZauberCMS.Core.Content.Models;
using ZauberCMS.Core.Data.Interfaces;

namespace ZauberCMS.Core.SeedData;

public class SyncTabsToUseAlias : ISeedData
{
    public void Initialise(IDocumentStore store)
    {
        var dbContext = store.OpenSession();
        // Get all ContentTypes
        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var types = dbContext.Query<ContentType>().ToList();
        foreach (var contentType in types)
        {
            // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage
            foreach (var contentProperty in contentType.ContentProperties)
            {
                if (string.IsNullOrWhiteSpace(contentProperty.TabAlias))
                {
                    // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataUsage
                    var tab = contentType.Tabs.FirstOrDefault(x => x.Id == contentProperty.TabId);
                    if (tab != null)
                    {
                        contentProperty.TabAlias = tab.Alias;
                    }
                }
            }
        }
            
        dbContext.SaveChanges();
    }
}