namespace Aero.EfCore;



public class AeroUserProfileEfCoreRepository(
    AeroDbContext context,
    ILogger<AeroUserProfileEfCoreRepository> log)
    : GenericEntityFrameworkRepository<AeroUserProfile>(context, log);