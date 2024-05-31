using Amazon.DynamoDBv2;
using Electra.Common.Extensions;
using Electra.Core.Entities;
using ServiceStack.Aws.DynamoDb;

namespace Electra.Persistence
{
    public interface IGenericPocoDynamoRepository<T> where T : Entity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> strategy, uint page = 1, uint rows = 10);
        Task<T> FindByIdAsync(string id);
        Task<T> InsertAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<T> UpsertAsync(T entity);
        Task DeleteAsync(string id);
        Task DeleteAsync(T entity);
        IEnumerable<T> GetAll();
        T FindById(String id);
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate, uint page = 1, uint rows = 10);
        T Insert(T entity);
        T Update(T entity);
        T Upsert(T entity);
        void Delete(String id);
        void Delete(T entity);
    }

    public class GenericPocoDynamoRepository<T> : GenericRepository<T, string>, IGenericPocoDynamoRepository<T> where T : Entity, IEntity<string>, new()
    {
        private readonly PocoDynamo db;
        private readonly string tableName;

        public GenericPocoDynamoRepository(IAmazonDynamoDB db, ILogger<GenericPocoDynamoRepository<T>> log) : base(log)
        {
            this.tableName = GetTableNameFromAttribute(typeof(T));
            this.db = new PocoDynamo(db); // encapsulated instead of injecting... 
            var meta = this.db.GetTableMetadata<T>();
            meta.Name = tableName;
        }

        public override async Task<long> CountAsync()
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public override async Task<bool> ExistsAsync(string id)
        {
            await Task.CompletedTask;
            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<T>> GetAllAsync()
        {
            log.LogInformation($"getting all records for table {tableName}");
            var results = await Task.Run(() => db.ScanAll<T>()?.ToList());
            log.LogInformation($"{results.Count()} results returned");
            return results;
        }

        public override async Task<T> GetByIdAsync(string id)
        {
            return await FindByIdAsync(id);
        }

        public override async Task<IReadOnlyCollection<T>> GetByIdsAsync(IEnumerable<string> ids)
        {
            await Task.Delay(0);

            throw new NotImplementedException();
        }

        public override async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate, uint page = 1, uint rows = 10)
        {
            // todo - can pass in a tuple that will accept three Func<T, bool> for use with main query, KeyCondition and Filter
            log.LogInformation($"searching w/ the following data: {predicate.Dump()}");

            var q = db.FromQuery(predicate);
            
            q.TableName = tableName;
            var results = await Task.Run(() => db.Query(q));
            return results;
        }
        
        Expression<Func<U, bool>> FuncToExpression<U>(Func<U, bool> func)  
        {  
            return x => func(x);  
        } 

        public override async Task<T> FindByIdAsync(string id)
        {
            log.LogInformation($"finding loan status rule with id {id}");
            var entity = db.GetItem<Entity>(id);

            if (entity == null)
                log.LogWarning($"unalbe to find the rule with the id {id}");
            else
                log.LogInformation($"found record with id {id}");
            
            return await Task.FromResult(entity as T);
        }
        
        public override async Task<T> InsertAsync(T entity)
        {
            log.LogInformation($"inserting record: {entity.Dump()}");
            var result =db.PutItem(entity);
            return await Task.FromResult(result);
        }

        public override async Task<T> UpdateAsync(T entity)
        {
            return await InsertAsync(entity);
        }

        public override async Task<T> UpsertAsync(T entity)
        {
            return await InsertAsync(entity);
        }

        public override async Task DeleteAsync(string id)
        {
            log.LogInformation($"deleting entity with id {id}");
            var did = new DynamoId();
            did.Hash = id;
            await Task.Run(() => db.DeleteItem<T>(did));
        }

        public override async Task DeleteAsync(T entity)
        {
            log.LogInformation($"deleting entity with {entity.Dump()}");
            var did = new DynamoId();
            did.Hash = entity;
            await Task.Run(() => db.DeleteItem<T>(did));
        }
        
        // todo - move this method into the Common.Utils static class or extension method
        protected string GetTableNameFromAttribute(Type t)
        {
            var attr = Attribute.GetCustomAttribute(t, typeof(DynamoTableNameAttribute)) as DynamoTableNameAttribute;
            if (attr == null || string.IsNullOrEmpty(attr.Name))
            {
                log.LogError($"unable to get the table name for DynamoDb table");
                return "";
            }

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            switch (env?.ToLower().First())
            {
                case 'd':
                    env = "dev";
                    break;
                case 's':
                    env = "stg";
                    break;
                case 'p':
                    env = "prd";
                    break;
            }
                    
            attr.Name = attr.Name.Replace("{env}", env);
            return attr.Name;
        }
    }
}