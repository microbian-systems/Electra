using System.Linq.Expressions;
using Raven.Client.Documents.Linq;
using ZauberCMS.Core.Languages.Models;

namespace ZauberCMS.Core.Languages.Parameters;

public class QueryLanguageParameters
{
    public List<string> Ids { get; set; } = [];
    public int PageIndex { get; set; } = 1;
    public int AmountPerPage { get; set; } = 10;
    public bool IncludeChildren { get; set; }
    public List<string> LanguageIsoCodes { get; set; } = [];
    public GetLanguageOrderBy OrderBy { get; set; } = GetLanguageOrderBy.DateCreatedDescending;
    public Expression<Func<Language, bool>>? WhereClause { get; set; }
    public Func<IRavenQueryable<Language>>? Query { get; set; }
}

public enum GetLanguageOrderBy
{
    DateCreated,
    DateCreatedDescending,
    LanguageIsoCode,
    LanguageCultureName
}