using System.Linq.Expressions;
using Electra.Core.Entities;
using Electra.Models.Entities;
using Electra.Persistence.Core;
using Electra.Persistence.Marten;

namespace Electra.Services;

public sealed class MartenUserProfileService<T>(
    IUserRepository userRepository,
    IGenericMartenRepository<T, string> db,
    ILogger<MartenUserProfileService<T>> log)
    : IUserProfileService<T>
    where T : ElectraUserProfile, new()
{
    public async Task<T> GetById(string id)
    {
        log.LogInformation($"getting user profile with id: {id}");
        return await db.FindByIdAsync(id);
    }

    public async Task<T> GetByEmail(string email) 
    {
        var user = (await userRepository.FindAsync(x => x.Email == email))
            .FirstOrDefault();

        if (user is null)
            return null;

        var profile = user.Profile;
        return (T)profile;
    }

    public async Task InsertAsync(T model)
    {
        log.LogInformation($"adding user: {model.ToJson()}");
        await db.InsertAsync(model);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(T model)
    {
        log.LogInformation($"updating user: {model.ToJson()}");
        await db.UpdateAsync(model);
        await db.SaveChangesAsync();
    }

    public async Task UpsertAsync(T model)
    {
        log.LogInformation($"upserting user: {model.ToJson()}");
        await db.UpsertAsync(model);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(T model) => await DeleteAsync(model.Id);

    public async Task DeleteAsync(string id)
    {
        log.LogWarning($"deleting user with id {id}");
        await db.DeleteAsync(id);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await db.FindAsync(predicate);
    }
}