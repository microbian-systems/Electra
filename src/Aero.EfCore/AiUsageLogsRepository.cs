using Aero.Core.Data;

namespace Aero.EfCore;

public interface IAiUsageLogRepository : IGenericRepository<AiUsageLog>;

public sealed class AiUsageLogsRepository(DbContext session, ILogger<AiUsageLogsRepository> log)
    : GenericEntityFrameworkRepository<AiUsageLog>(session, log), IAiUsageLogRepository;