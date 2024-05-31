using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Electra.Core.Entities;

namespace Electra.Persistence
{
    public interface IGenericDynamoRepository<T> where T : Entity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> FindByIdAsync(string id);
        Task<T> InsertAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<T> UpsertAsync(T entity); 
        Task DeleteAsync(string id);
        Task DeleteAsync(T entity);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> strategy, uint page = 1, uint rows = 10);
        IEnumerable<T> GetAll();
        T FindById(string id);
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate, uint page = 1, uint rows = 10);
        T Insert(T entity);
        T Update(T entity);
        T Upsert(T entity);
        void Delete(string id);
        void Delete(T entity);
    }

    public class GenericDynamoRepository<T> : GenericRepository<T, string>, IGenericDynamoRepository<T> where T : Entity, IEntity<string>, new()
    {
        protected readonly IAmazonDynamoDB client;
        protected readonly string tableName;
        protected readonly Table table;
        protected readonly DynamoDBContext db;
        protected readonly DynamoDBContextConfig contextConfig;
        protected readonly DynamoDBOperationConfig opConfig;

        public GenericDynamoRepository(IAmazonDynamoDB client, ILogger<GenericDynamoRepository<T>> log) : base(log)
        {
            this.client = client;
            tableName = GetTableNameFromAttribute(typeof(T));
            this.table = Table.LoadTable(client, tableName);  // todo - check if resource exists CreateIfNotExists(tableName);
            this.contextConfig = new DynamoDBContextConfig{}; // todo - find out best parameters for dynamo config
            this.opConfig = new DynamoDBOperationConfig
            {
                OverrideTableName = tableName
            };
            this.db = new DynamoDBContext(client, contextConfig);
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
            var conditions = new List<ScanCondition>();
            // todo - verify this dynamodb search (scan) is scalable 
            var results =  await db.ScanAsync<T>(conditions, opConfig).GetRemainingAsync();
            return results;
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

        /// <summary>
        /// Attemps to find data from the dynamodb database
        /// </summary>
        /// <param name="strategy">a tuple representing the following structure (QueryRequest request, string hash, string range)"/></param>
        /// <typeparam name="S">(QueryRequest, string, string)</typeparam>
        /// <returns>a return type of generic type R (actually IEnumerable until changed)</returns>
        public override async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> strategy, uint page = 1, uint rows = 10) // todo - change this to have another generic return type 'R'
        {
            throw new NotImplementedException();
            
            //object obj = strategy;
            //(var request, var hash, var range)  = ((QueryRequest, string, string)) obj;
            //var result = await InternalFindAsync(request, hash, range);
            //return result;
        }
        
        protected virtual async Task<IEnumerable<T>> InternalFindAsync(QueryRequest request, 
            string hashKey="Application", string rangeKey="Version")
        {
            var response = await client.QueryAsync(request);
            var app = response.Items[0].FirstOrDefault(s => s.Key == hashKey).Value.S;
            var version = response.Items[0].FirstOrDefault(s => s.Key == rangeKey).Value.S;
            var result = await db.LoadAsync<T>(app, version, opConfig);
            return new[] {result};
        }

        public override async Task<T> FindByIdAsync(string id)
        {
            log.LogInformation($"retrieving record with id {id}");
            return await db.LoadAsync<T>(id, "rules");
        }
        
        public override async Task<T> InsertAsync(T entity)
        {
            await db.SaveAsync(entity, opConfig);
            return entity;
        }

        public override async Task<T> UpdateAsync(T entity)
        {
            await db.SaveAsync(entity, opConfig);
            return entity;
        }

        public override async Task<T> UpsertAsync(T entity)
        {
            await db.SaveAsync(entity, opConfig);
            return entity;
        }

        public override async Task DeleteAsync(string id)
        {
            log.LogInformation($"deleing object from dynamo with id of {id}");
            await db.DeleteAsync<T>(id, opConfig);
        }

        public override async Task DeleteAsync(T entity) => await DeleteAsync(entity.Id.ToString());

        // todo - move this method into the Common.Utils static class or extension method
        protected string GetTableNameFromAttribute(Type t)
        {
            var attr = Attribute.GetCustomAttribute(t, typeof(DynamoTableNameAttribute)) as DynamoTableNameAttribute;
            if (attr == null || string.IsNullOrEmpty(attr.Name))
            {
                log.LogError($"unable to get the table name for DynamoDb table");
                return "";
            }

            // todo - do we really need the env for dynamo table names
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
                    
            attr.Name = attr?.Name.Replace("{env}", env);
            return attr?.Name;
        }
    }
}