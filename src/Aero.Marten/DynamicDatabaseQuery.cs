using Aero.Core.Entities;
using Serilog;

namespace Aero.Marten;

public class DynamicDatabaseQuery<T> : IDynamicDatabaseQuery<T> where T : class, IEntity<Guid>
{
    private readonly ILogger log;
    private readonly IDocumentSession db;

    public DynamicDatabaseQuery(IDocumentSession db, ILogger log)
    {
        this.db = db;
        this.log = log;
    }

    public async Task<IEnumerable<T>> ExecuteAsync(Expression<Func<T, bool>> parameter)
    {
        log.Information($"querying database ...");
        var results = await db.Query<T>().Where(parameter).ToListAsync();
        log.Information($"finished query database with {results.Count} results");
        return results;
    }
}