using System.Linq.Expressions;
using Electra.Core.Entities;
using Electra.Persistence.EfCore;
using Electra.Persistence.Repositories;

namespace Electra.Services;

public interface IElectraUserProfileService : IUserProfileService<ElectraUserProfile>{}

public interface IElectraUserProfileServiceRepository : IGenericEntityFrameworkRepository<ElectraUserProfile, Guid>{}
public class ElectraUserProfileServiceRepository : GenericEntityFrameworkRepository<ElectraUserProfile, Guid>, IElectraUserProfileServiceRepository
{
    public ElectraUserProfileServiceRepository(ElectraDbContext context, ILogger<ElectraUserProfileServiceRepository> log) : base(context, log)
    {
    }
}
public class ElectraUserProfileService : UserProfileService<ElectraUserProfile>, IElectraUserProfileService
{
    public ElectraUserProfileService(IElectraUserProfileServiceRepository db, ILogger<ElectraUserProfileService> log) : base(db, log)
    {
    }
}

public interface IUserProfileService<T> where T : ElectraUserProfile, IEntity<Guid>
{
    Task<T> GetById(Guid id);
    Task<T> GetByEmail(string email);
    Task InsertAsync(T model);
    Task UpdateAsync(T model);
    Task UpsertAsync(T model);
    Task DeleteAsync(T model);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}
    
public class UserProfileService<T> : IUserProfileService<T>
    where T : ElectraUserProfile, new()
{
    private readonly IGenericRepository<T, Guid> db;
    private readonly ILogger<UserProfileService<T>> log;

    public UserProfileService(IGenericRepository<T, Guid> db, ILogger<UserProfileService<T>> log)
    {
        this.db = db;
        this.log = log;
    }
        
    public async Task<T> GetById(Guid id)
    {
        var results = await db.FindByIdAsync(id);
        return results;
    }

    public async Task<T> GetByEmail(string email)
    {
        var results = await db.FindAsync(x => x.Email.ToUpper() == email);
        return results?.First();
    }

    public async Task InsertAsync(T model)
    {
        var res = await db.InsertAsync(model);
    }

    public async Task UpdateAsync(T model)
    {
        var res = await db.UpdateAsync(model);
    }

    public async Task UpsertAsync(T model)
    {
        var res = await db.UpsertAsync(model);
    }

    public async Task DeleteAsync(T model)
    {
        await DeleteAsync(model.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await db.DeleteAsync(id);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var results = await db.FindAsync(predicate);
        return results;
    }
}