using System.Linq.Expressions;
using Raven.Client.Documents.Linq;
using ZauberCMS.Core.Content.Models;

namespace ZauberCMS.Core.Content.Parameters;

public class QueryDomainParameters
{
    string? Id {get; set;} 
    public List<string> Ids { get; set; } = [];
    public int PageIndex { get; set; } = 1;
    public int AmountPerPage { get; set; } = 10;
    public bool IncludeChildren { get; set; }
    public string? ContentId { get; set; }
    public string? LanguageId { get; set; }
    public GetDomainOrderBy OrderBy { get; set; } = GetDomainOrderBy.DateCreatedDescending;
    public Expression<Func<Domain, bool>>? WhereClause { get; set; }
    public Func<IRavenQueryable<Domain>>? Query { get; set; }
}

public enum GetDomainOrderBy
{
    DateCreated,
    DateCreatedDescending,
    Url
}