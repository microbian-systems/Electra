using Electra.Models.Entities;
using Electra.Persistence.Core.EfCore;

namespace Electra.Persistence;

public interface IElectraUserProfileRepository : IGenericEntityFrameworkRepository<ElectraUserProfile>{}

public class ElectraUserProfileRepository(
    ElectraDbContext context,
    ILogger<ElectraUserProfileRepository> log)
    : GenericEntityFrameworkRepository<ElectraUserProfile>(context, log), IElectraUserProfileRepository;