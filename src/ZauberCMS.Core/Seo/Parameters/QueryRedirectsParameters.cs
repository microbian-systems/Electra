using Raven.Client.Documents.Linq;
using ZauberCMS.Core.Seo.Models;

namespace ZauberCMS.Core.Seo.Parameters;

public class QueryRedirectsParameters
{
    public bool Cached { get; set; } = true;
    public List<string> Ids { get; set; } = [];
    public int Amount { get; set; } = 5000;
    public GetSeoRedirectOrderBy OrderBy { get; set; } = GetSeoRedirectOrderBy.DateUpdatedDescending;
    public Func<IRavenQueryable<SeoRedirect>>? Query { get; set; }
}

public enum GetSeoRedirectOrderBy
{
    DateCreated,
    DateCreatedDescending,
    DateUpdated,
    DateUpdatedDescending,
    FromUrl
}