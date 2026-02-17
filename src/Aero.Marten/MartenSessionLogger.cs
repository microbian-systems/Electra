using Marten.Services;
using Npgsql;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Aero.Marten;

public class MartenSessionLogger : IMartenSessionLogger
{
    // todo - consider DI for the Serilog.ILogger
    private readonly ILogger log = Log.Logger;

    public void LogSuccess(NpgsqlCommand command)
    {
        log.Information($"npgsql command successful {command.CommandType}: {command.CommandText}");
    }

    public void LogFailure(NpgsqlCommand command, Exception ex)
    {
        log.Error($"npgsql command successful {command.CommandType}: {command.CommandText}");
    }

    public void LogSuccess(NpgsqlBatch batch)
    {
        log.Information("batch update success");
    }

    public void LogFailure(NpgsqlBatch batch, Exception ex)
    {
        log.Error(ex, "error with batch command");
    }

    public void LogFailure(Exception ex, string message)
    {
        log.Error(ex, message);
    }

    public void RecordSavedChanges(IDocumentSession session, IChangeSet commit)
    {
        log.Information($@"saved changes successful
                                 Inserts: {commit.Inserted}
                                 Updates: {commit.Updated}
                                 Deletes: {commit.Deleted}
                             ");
    }

    public void OnBeforeExecute(NpgsqlCommand command)
    {
    }

    public void OnBeforeExecute(NpgsqlBatch batch)
    {
    }
}