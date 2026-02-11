using Electra.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.EfCore;

public interface IElectraUserProfileRepository : IGenericEntityFrameworkRepository<ElectraUserProfile>{}

public class ElectraUserProfileRepository(
    ElectraDbContext context,
    ILogger<ElectraUserProfileRepository> log)
    : GenericEntityFrameworkRepository<ElectraUserProfile>(context, log), IElectraUserProfileRepository;