using System.Linq.Expressions;
using Raven.Client.Documents.Linq;

namespace ZauberCMS.Core.Audit.Parameters;

public class QueryAuditsParameters
{
    public string? Id {get; set;} 
    public int PageIndex { get; set; } = 1;
    public int AmountPerPage { get; set; } = 10;
    public string? Username { get; set; }
    public GetAuditsOrderBy OrderBy { get; set; } = GetAuditsOrderBy.DateCreatedDescending;
    public Expression<Func<Models.Audit, bool>>? WhereClause { get; set; }
    public Func<IRavenQueryable<Models.Audit>>? Query { get; set; }
}

public enum GetAuditsOrderBy
{
    DateCreated,
    DateCreatedDescending,
    Username
}