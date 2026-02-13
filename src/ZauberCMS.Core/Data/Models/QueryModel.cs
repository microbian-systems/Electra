using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using ZauberCMS.Core.Data.Interfaces;

namespace ZauberCMS.Core.Data.Models;

public class QueryModel<T> : IQueryModel
{
    public string? Name { get; set; }
    public Func<IAsyncDocumentSession, IRavenQueryable<T>> Query { get; set; } = null!;

    public async Task<IEnumerable<object>> ExecuteQuery(IAsyncDocumentSession session, CancellationToken cancellationToken)
    {
        var queryResult = await Query(session).ToListAsync(cancellationToken);
        return queryResult.Cast<object>();
    }
}