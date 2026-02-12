using Raven.Client.Documents.Session;

namespace ZauberCMS.Core.Data.Interfaces;

public interface IQueryModel
{
    string? Name { get; }
    Task<IEnumerable<object>> ExecuteQuery(IAsyncDocumentSession dbContext, CancellationToken cancellationToken);
}