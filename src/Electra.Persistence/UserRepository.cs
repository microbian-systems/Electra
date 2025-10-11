using Electra.Models.Entities;
using Electra.Persistence.EfCore;

namespace Electra.Persistence;

public interface IUserRepository : IGenericEntityFrameworkRepository<ElectraUser>;

public class UserRepository(DbContext context, ILogger<GenericEntityFrameworkRepository<ElectraUser>> log) 
    : GenericEntityFrameworkRepository<ElectraUser>(context, log)
{
    
}