using System;
using System.Threading.Tasks;
using Electra.Core.Entities;
using Marten;
using Serilog;

namespace Electra.Persistence.Marten;

public class SaveToDbCommand<T> : ISaveToDbCommand<T> where T : Entity<string>, IEntity<string>
{
    private readonly ILogger log;
    private readonly IDocumentSession db;

    public SaveToDbCommand(IDocumentSession db, ILogger log)
    {
        this.db = db;
        this.log = log;
    }
        
    public async Task<T> ExecuteAsync(T parameter)
    {
        log.Information($"saving {parameter.GetType()} to database");
        db.Store(parameter);
        parameter.ModifiedOn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var message = $"successfully saved {parameter.GetType()} to database with id {parameter.Id}";
        log.Information(message);

        return parameter;
    }
}