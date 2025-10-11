using System.Linq.Expressions;
using Electra.Core.Entities;
using Electra.Models.Entities;
using Electra.Persistence.EfCore;
using Electra.Persistence.Repositories;

namespace Electra.Services;

public interface IElectraUserProfileService : IUserProfileService<ElectraUserProfile>{}

public class ElectraUserProfileService(IElectraUserProfileRepository db, ILogger<ElectraUserProfileService> log)
    : UserProfileService<ElectraUserProfile>(db, log), IElectraUserProfileService;

public interface IUserProfileService<T> where T : ElectraUserProfile, IEntity
{
    Task<T> GetById(long id);
    Task<T> GetByEmail(string email);
    Task InsertAsync(T model);
    Task UpdateAsync(T model);
    Task UpsertAsync(T model);
    Task DeleteAsync(T model);
    Task DeleteAsync(long id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
}
    
public class UserProfileService<T>(IGenericRepository<T> db, ILogger<UserProfileService<T>> log)
    : IUserProfileService<T>
    where T : ElectraUserProfile, new()
{
    private readonly ILogger<UserProfileService<T>> log = log;

    public async Task<T> GetById(long id)
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

    public async Task DeleteAsync(long id)
    {
        await db.DeleteAsync(id);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var results = await db.FindAsync(predicate);
        return results;
    }
}