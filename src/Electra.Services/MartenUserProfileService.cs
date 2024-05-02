using System.Linq.Expressions;

namespace Electra.Services
{
    public sealed class MartenUserProfileService<T> : IUserProfileService<T> where T : AppXUserProfile, IEntity<string>, new()
    {
        private readonly ILogger<MartenUserProfileService<T>> log;
        private readonly IGenericMartenRepository<T, string> db;

        public MartenUserProfileService(IGenericMartenRepository<T, string> db, ILogger<MartenUserProfileService<T>> log)
        {
            this.db = db;
            this.log = log;
        }

        public async Task<T> GetById(string id)
        {
            log.LogInformation($"getting user profile with id: {id}");
            return await db.FindByIdAsync(id);
        }

        public async Task<T> GetByEmail(string email)
        {    
            var result = await db.FindAsync(x => x.Email == email);

            if (result.Any())
                return result?.First();

            return null;
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
}