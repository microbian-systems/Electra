using System.ComponentModel;
using Electra.Common.Extensions;
using Electra.Persistence.Entities;
using Nest;

namespace Electra.Persistence
{
    public interface IElasticsearchRepository<T> where T : ElasticEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> FindByIdAsync(string id);
        Task<T> InsertAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<T> UpsertAsync(T entity);
        Task DeleteAsync(string id);
        Task DeleteAsync(T entity);
        IEnumerable<T> Find(SearchDescriptor<T> descriptor);
        Task<IEnumerable<T>> FindAsync(SearchDescriptor<T> descriptor);
        IEnumerable<T> GetAll();
        T FindById(String id);
        T Insert(T entity);
        T Update(T entity);
        T Upsert(T entity);
        void Delete(String id);
        void Delete(T entity);
    }

    // tutorial - https://code-maze.com/elasticsearch-aspnet-core/
    // official docs: https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/term-level-queries.html
    // indexes, etc: https://www.toptal.com/dot-net/elasticsearch-dot-net-developers
    // indexes exist: https://www.c-sharpcorner.com/article/working-on-elasticsearch-using-net-nest/
    // reindexing: https://www.blexin.com/en-US/Article/Blog/How-to-integrate-ElasticSearch-in-ASPNET-Core-70
    // misc: https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/writing-queries.html
    public class ElasticsearchRepository<T> : GenericRepository<T, string>, IElasticsearchRepository<T> where T : ElasticEntity, new()
    {
        private const string message = "this method is not available for elasticsearch repository";
        private readonly IElasticClient client;

        public ElasticsearchRepository(IElasticClient client, ILogger<ElasticsearchRepository<T>> log) : base(log)
        {
            this.client = client;
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
            log.LogInformation($"retrieving all recorsd for type {typeof(T)}");
            var response = await client.SearchAsync<IEntity<string>>(s => s.MatchAll());
            return response.Documents.AsEnumerable().Cast<T>();
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
            log.LogInformation($"retrieving document with id {id}");
            var result = await client.GetAsync<T>(id);
            log.LogInformation($"success: {result.Found}");

            return result.Source;
        }

        public override async Task<T> InsertAsync(T entity)
        {
            log.LogInformation($"inserting entity: {entity.ToJson()}");
            var response = await client.IndexDocumentAsync(entity);
            if (!response.IsValid)
                throw new InvalidEnumArgumentException(response.ServerError.Error.Reason);

            log.LogInformation($"entity inserted.");
            return entity;
        }

        public override async Task<T> UpdateAsync(T entity)
        {
            /*
             * can use new {fieldName = "value"} to do partial udpates
             */
            log.LogInformation($"updating entity {entity.ToJson()}");

            var response = await client.UpdateAsync<T, object>(entity.Id, d => d.Doc(entity)
                .RetryOnConflict(3));
            return entity;
        }

        public override async Task<T> UpsertAsync(T entity)
        {
            log.LogInformation($"upserting entity {entity.ToJson()}");

            // var exists = await FindByIdAsync(entity.Id);
            // if (exists != null)
            // {
            //     await UpdateAsync(entity);
            // }
            // else
            // {
            //     var response = await InsertAsync(entity);
            // }

            var response = await client.UpdateAsync<T, object>(entity.Id, d => d.DocAsUpsert()
                .RetryOnConflict(3));

            return entity;
        }

        public override async Task DeleteAsync(string id)
        {
            log.LogInformation($"deleting item with id: {id}");
            var response = await client.DeleteAsync<T>(id);
        }

        public override async Task DeleteAsync(T entity)
        {
            log.LogInformation($"deleting entity: {entity.ToJson()}");
            var response = await client.DeleteAsync<T>(entity.Id);
        }

        public IEnumerable<T> Find(SearchDescriptor<T> descriptor)
            => FindAsync(descriptor).GetAwaiter().GetResult();

        public async Task<IEnumerable<T>> FindAsync(SearchDescriptor<T> descriptor)
        {
            // var response = await client.SearchAsync<T>(s => s   // paged search
            //     .From(0)
            //     .Size(20)
            //     .Query(q => q 
            //         .Match(m => m
            //             .Field(f => f.Id == id)
            //             .Query("get_all")
            //         )
            //     )
            // );
            //  -----------------  OR ---------------------
            // example query
            // var d = new SearchDescriptor<T>();  // all matching docs
            // d.From(0).Size(1).Query(q => q
            //     .Match(m => m
            //         .Field(f => f.Id == "test")
            //     )
            // );
            //  -----------------  OR ---------------------
            // var start = page <= 1
            //     ? 0
            //     : (page-1) * size;
            //
            // var descriptor = new SearchDescriptor<UserActivityModel>()  // exact match
            //         .From(start)
            //         .Size(size)
            //         .Query(q => q
            //             .Term(t => t
            //                 .Field(f => f.UserId)
            //                 .Value(userId)
            //             )
            //         )
            //     ;

            log.LogInformation($"searching elastic ...");

            var response = await client.SearchAsync<T>(descriptor);
            var results = response.Documents;

            return results;
        }
        
        #region Unused
        /// <summary>
        /// This method is not available for Elasticsearch. Please use the overriden method(s)
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        //[Obsolete("this method isn't applicable for Elasticsearch", true)]
        public override Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}