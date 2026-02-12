using Electra.Models.Entities;
using Electra.Persistence.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.EfCore;

public interface IAiUsageLogRepository : IGenericRepository<AiUsageLog>;

public sealed class AiUsageLogsRepository(DbContext session, ILogger<AiUsageLogsRepository> log)
    : GenericEntityFrameworkRepository<AiUsageLog>(session, log), IAiUsageLogRepository;