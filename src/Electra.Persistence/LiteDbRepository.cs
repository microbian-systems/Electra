using Electra.Core.Entities;
using LiteDB;

namespace Electra.Persistence
{
    public class LiteDbRepository<T> : GenericRepository<T, string> where T : Entity<string>, new()
    {
        private readonly ILiteDatabase store;
        private readonly ILiteCollection<T> db;

        public LiteDbRepository(ILiteDatabase store, ILogger log) : base(log)
        {
            this.store = store;
            this.db = store.GetCollection<T>();
        }

        public override async Task<long> CountAsync()
        {
            await Task.Delay(0);

            throw new NotImplementedException();
        }

        public override async Task<bool> ExistsAsync(string id)
        {
            await Task.Delay(0);

            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<T>> GetAllAsync()
        {
            var results = db.FindAll();
            return await Task.FromResult(results);
        }

        public override async Task<T> GetByIdAsync(string id)
        {
            await Task.Delay(0);

            throw new NotImplementedException();
        }

        public override async Task<IReadOnlyCollection<T>> GetByIdsAsync(IEnumerable<string> ids)
        {
            await Task.Delay(0);

            throw new NotImplementedException();
        }

        public override async Task<T> FindByIdAsync(string id)
        {
            var entity = db.FindById(id);
            return await Task.FromResult(entity);
        }

        public override async Task<T> InsertAsync(T entity)
        {
            db.Insert(entity);
            return await Task.FromResult(entity);
        }

        public override async Task<T> UpdateAsync(T entity)
        {
            db.Update(entity);
            return await Task.FromResult(entity);
        }

        public override async Task<T> UpsertAsync(T entity)
        {
            if (!string.IsNullOrEmpty(entity.Id) && db.Find(x => x.Id == entity.Id).Any())
            {
                db.Update(entity);
                return await Task.FromResult(entity);
            }
            
            db.Insert(entity);
            return await Task.FromResult(entity);
        }

        public override async Task DeleteAsync(string id)
        {
            db.Delete(id);
        }

        public override async Task DeleteAsync(T entity)
        {
            db.Delete(entity.Id);
        }

        public override async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate, uint page = 1, uint rows = 10)
        {
            var results = db.Find(predicate);
            return await Task.FromResult(results);
        }
        
        public Expression<Func<U, bool>> FuncToExpression<U>(Func<U, bool> func)  
        {  
            return x => func(x);  
        }  
    }
}