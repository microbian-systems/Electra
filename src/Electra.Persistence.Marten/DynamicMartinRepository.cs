using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Electra.Core.Entities;
using Electra.Persistence.Core;
using Marten;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.Marten;

// todo - consider placing a constraint on type TKey for the marten repositories
public interface IDynamicMartenRepository : IDynamicRepositoryAsync<string>
{
}

public class DynamicMartinRepository(IDocumentSession db, ILogger<DynamicMartinRepository> log)
    : IDynamicMartenRepository
{
    private readonly ILogger<DynamicMartinRepository> log = log;

    // todo - implement InvalidateCache for marten repo
    public async Task InvalidateCacheAsync<T>(IEnumerable<T> documents) where T : class, IEntity<string>, new()
    {
        throw new NotImplementedException();
    }

    public async Task<long> CountAsync<T>() where T : class, IEntity<string>, new()
    {
        return await db.Query<T>().CountAsync(CancellationToken.None)
            .ConfigureAwait(false);
    }

    public async Task<T> GetByIdAsync<T>(string id) where T : class, IEntity<string>, new()
    {
        return await db.Query<T>().FirstAsync(x => Equals(x.Id, id)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<T>> GetByIdsAsync<T>(List<string> ids) where T : class, IEntity<string>, new()
    {
        var batch = db.CreateBatchQuery();
        var res = await batch.LoadMany<T>().ByIdList(ids);
        await batch.Execute();

        return new ReadOnlyCollection<T>(res.ToArray());
    }

    public async Task<T> FindSingle<T>(Expression<Func<T, bool>> predicate) where T : class, IEntity<string>, new()
    {
        return await db.Query<T>().FirstOrDefaultAsync<T>(predicate);
    }

    Expression<Func<T, bool>> FuncToExpression<T>(Func<T, bool> func)
    {
        return x => func(x);
    }

    public async Task<IEnumerable<T>> Search<T>(Expression<Func<T, bool>> predicate)
        where T : class, IEntity<string>, new()
    {
        var results = (await db.Query<T>().Where(predicate)
                .ToListAsync())
            .AsEnumerable();

        return results;
    }

    // todo - add FindAllAsync(Func<> predicate) or Where(Func<> predicate)
    public async Task<IEnumerable<T>> GetAllAsync<T>() where T : class, IEntity<string>, new()
    {
        return await db.Query<T>().ToListAsync(CancellationToken.None);
    }

    public async Task<bool> ExistsAsync<T>(string id) where T : class, IEntity<string>, new()
    {
        var res = await db.Query<T>().FirstAsync(x => Equals(x.Id, id));
        return res != null;
    }

    protected async Task<T> AddAsync<T>(T document) where T : class, IEntity<string>, new()
    {
        db.Store(document);
        await db.SaveChangesAsync();
        return document; // todo - verify martne reutrns a new id after saving
    }

    protected async Task AddAsync<T>(IEnumerable<T> documents) where T : class, IEntity<string>, new()
    {
        db.Store(documents);
        await db.SaveChangesAsync();
    }

    public async Task<T> SaveAsync<T>(T document) where T : class, IEntity<string>, new()
    {
        var res = await AddAsync(document);
        return res;
    }

    public async Task SaveAsync<T>(IEnumerable<T> documents) where T : class, IEntity<string>, new()
    {
        await AddAsync(documents);
    }

    public async Task DeleteAsync<T>(string id) where T : class, IEntity<string>, new()
    {
        db.Delete<T>(id);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync<T>(List<string> ids) where T : class, IEntity<string>, new()
    {
        // todo - fix batch deletes for marten
        foreach (var id in ids)
            db.Delete<T>(id);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync<T>(T document) where T : class, IEntity<string>, new()
    {
        db.Delete<T>(document);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync<T>(IEnumerable<T> documents) where T : class, IEntity<string>, new()
    {
        // todo - fix batch deleteds for marten
        foreach (var doc in documents)
            db.Delete<T>(doc);
        await db.SaveChangesAsync();
    }
}