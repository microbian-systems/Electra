using System.Linq.Expressions;
using Raven.Client.Documents.Linq;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Parameters;

public class QueryRolesParameters
{
    public List<string> Roles { get; set; } = [];
    
    public List<string> Ids { get; set; } = [];
    public int PageIndex { get; set; } = 1;
    public int AmountPerPage { get; set; } = 10;
    public GetRolesOrderBy OrderBy { get; set; } = GetRolesOrderBy.DateUpdatedDescending;
    public Expression<Func<Role, bool>>? WhereClause { get; set; }
    public Func<IRavenQueryable<Role>>? Query { get; set; }
    public string? SearchTerm { get; set; }
}

public enum GetRolesOrderBy
{
    DateUpdated,
    DateUpdatedDescending,
    DateCreated,
    DateCreatedDescending,
    Name,
    NameDescending
}