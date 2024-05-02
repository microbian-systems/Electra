using Marten;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Microbians.Persistence
{
    public class MartenLogger : IMartenLogger
    {
        private readonly ILogger log = Log.Logger;
        public IMartenSessionLogger SessionLog { get; }

        public MartenLogger(IMartenSessionLogger sessionLog = null)
        {
            this.SessionLog = sessionLog ?? new MartenSessionLogger();
        }
        
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
}