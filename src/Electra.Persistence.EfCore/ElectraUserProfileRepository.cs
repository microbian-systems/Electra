namespace Electra.Persistence.EfCore;



public class ElectraUserProfileEfCoreRepository(
    ElectraDbContext context,
    ILogger<ElectraUserProfileEfCoreRepository> log)
    : GenericEntityFrameworkRepository<ElectraUserProfile>(context, log), IElectraUserProfileRepository;