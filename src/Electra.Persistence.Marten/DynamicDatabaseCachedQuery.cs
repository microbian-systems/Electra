using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Electra.Core.Entities;
using Foundatio.Caching;
using Serilog;

namespace Electra.Persistence.Marten
{
    public class DynamicDatabaseCachedQuery<T> : IDynamicDatabaseCachedQuery<T> where T : class, IEntity<Guid>
    {
        private readonly IDynamicDatabaseQuery<T> query;
        private readonly ILogger log;
        private readonly ICacheClient cache;

        public DynamicDatabaseCachedQuery(ICacheClient cache, IDynamicDatabaseQuery<T> query, ILogger log)
        {
            this.query = query;
            this.log = log;
            this.cache = cache;
        }
        
        public async Task<IEnumerable<T>> ExecuteAsync(Expression<Func<T, bool>> parameter)
        {
            log.Information($"attempting to retrieved cached query....");
            var key = parameter.Name;
            if (await cache.ExistsAsync(key))
            {
                log.Information($"cache hit. results found");
                return (await cache.GetAsync<T>(key)).Value as IEnumerable<T>;
            }
            
            log.Information($"cache miss.  attempting to get and store results");
            var results = await query.ExecuteAsync(parameter);
            log.Information($"results found: {results.Count()}");
            await cache.AddAsync(key, results, TimeSpan.FromMinutes(5));
            log.Information($"added results to cache");
            return results;
        }
    }
}