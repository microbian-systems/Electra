using Electra.Models.Entities;
using Electra.Persistence.EfCore;
using Electra.Persistence.Repositories;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence;

public interface IAiUsageLogRepository : IGenericRepository<AiUsageLog>;

public sealed class AiUsageLogsRepository(ElectraDbContext context, ILogger<AiUsageLogsRepository> log)
    : GenericEntityFrameworkRepository<AiUsageLog>(context, log), IAiUsageLogRepository;