using Marten;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Electra.Persistence.Marten;

public class MartenLogger(IMartenSessionLogger sessionLog) : IMartenLogger
{
    private readonly ILogger log = Log.Logger;
    public IMartenSessionLogger SessionLog { get; } = sessionLog ?? new MartenSessionLogger();

    public IMartenSessionLogger StartSession(IQuerySession session)
    {
        // todo - figure out how to use IQuerySession obj in MartenLogger
        return SessionLog;
    }

    public void SchemaChange(string sql)
    {
        Log.Information($"there was a session chagne {sql}");
    }
}