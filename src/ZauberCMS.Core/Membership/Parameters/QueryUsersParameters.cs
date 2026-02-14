using System.Linq.Expressions;
using Electra.Models.Entities;
using Raven.Client.Documents.Linq;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Parameters;

public class QueryUsersParameters
{
    public bool Cached { get; set; }
    public List<string> Roles { get; set; } = [];
    
    public List<string> Ids { get; set; } = [];
    public int PageIndex { get; set; } = 1;
    public int AmountPerPage { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public GetUsersOrderBy OrderBy { get; set; } = GetUsersOrderBy.DateUpdatedDescending;
    public Expression<Func<ElectraUser, bool>>? WhereClause { get; set; }
    public Func<IRavenQueryable<ElectraUser>>? Query { get; set; }
}

public enum GetUsersOrderBy
{
    DateUpdated,
    DateUpdatedDescending,
    DateCreated,
    DateCreatedDescending
}