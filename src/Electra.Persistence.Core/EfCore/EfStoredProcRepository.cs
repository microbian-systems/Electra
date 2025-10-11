using Electra.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;

namespace Electra.Persistence.EfCore;

// todo - impl complete logging
public class EfStoredProcRepository(DbContext ctx, ILogger log) : IStoredProcRepository
{
    protected DbContext db = ctx ?? throw new ArgumentNullException($"Database Context parameter was null inside {nameof(EfStoredProcRepository)}");

    public void ExecStoredProc(string name, params object[] parameters)
    {
        var result = ExecStoredProc<object>(name, parameters);
    }


    // todo - find alternative to SqlQuery Async
    public object ExecStoredProc<U>(string name, params object[] parameters)
    {
        if (db == null)
            throw new ArgumentNullException($"database context cannot be null");

        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException($"stored procedure {nameof(name)} parameter was null or empty");

        log.Verbose($"Executing stored procedure: {name} of type {typeof(U)}");
        // todo - fix -> var result = db.Database.SqlQuery<U>($"{name}", parameters);
        var result = new object();
        return result;
    }

    public async Task ExecStoredProcAsync(string name, params object[] parameters)
    {
        var result = await ExecStoredProcAsync<object>(name, parameters);
    }


    // todo - find alternative to SqlQuery Async
    public async Task<object> ExecStoredProcAsync<U>(string name, params object[] parameters)
    {
        if (db == null)
            throw new ArgumentNullException($"database context cannot be null");

        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException($"stored procedure {nameof(name)} parameter was null or empty");

        // todo - fix -> var result = db.Database.SqlQuery<U>($"exec {name}", parameters);
        var result = new object();
        return await Task.FromResult(result);
    }
}