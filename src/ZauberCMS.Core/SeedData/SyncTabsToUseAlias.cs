using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using ZauberCMS.Core.Content.Models;
using ZauberCMS.Core.Data.Interfaces;

namespace ZauberCMS.Core.SeedData;

public class SyncTabsToUseAlias : ISeedData
{
    public void Initialise(IDocumentSession db)
    {
        // Get all ContentTypes
        // ReSharper disable once EntityFramework.NPlusOne.IncompleteDataQuery
        var types = db.Query<ContentType>().ToList();
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
            
        db.SaveChanges();
    }
}