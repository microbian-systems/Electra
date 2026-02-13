using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using ZauberCMS.Core.Data.Interfaces;
using ZauberCMS.Core.Data.Models;
using ZauberCMS.Core.Data.Parameters;
using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Plugins;
using ZauberCMS.Core.Shared.Interfaces;
using ZauberCMS.Core.Shared.Models;
using ZauberCMS.Core.Shared.Services;

namespace ZauberCMS.Core.Data.Services;

public class DataService(
    IServiceScopeFactory serviceScopeFactory,
    ICacheService cacheService,
    ExtensionManager extensionManager)
    : IDataService
{
    /// <summary>
    /// Retrieves global data by alias, optionally from cache.
    /// </summary>
    /// <param name="parameters">Alias and caching flag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Global data or null.</returns>
    public async Task<GlobalData?> GetGlobalDataAsync(GetGlobalDataParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        var query = BuildQuery(parameters, dbContext);
        var cacheKey = query.GenerateCacheKey(typeof(GlobalData));
        
        if (parameters.Cached)
        {
            return await cacheService.GetSetCachedItemAsync(cacheKey, 
                async () => await query.FirstOrDefaultAsync(cancellationToken));
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Creates or updates a global data value by alias.
    /// </summary>
    /// <param name="parameters">Alias and value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result including success and messages.</returns>
    public async Task<HandlerResult<GlobalData>> SaveGlobalDataAsync(SaveGlobalDataParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        
        var handlerResult = new HandlerResult<GlobalData>();

        if (!parameters.Alias.IsNullOrWhiteSpace() && !parameters.Data.IsNullOrWhiteSpace())
        {
            // Get the DB version
            var gData = dbContext.Query<GlobalData>()
                .FirstOrDefault(x => x.Alias == parameters.Alias);

            if (gData == null)
            {
                gData = new GlobalData{Alias = parameters.Alias, Data = parameters.Data};
                await dbContext.StoreAsync(gData, cancellationToken);
            }
            else
            {
                gData.Data = parameters.Data;   
                gData.DateUpdated = DateTime.UtcNow;
            }
            
            return await dbContext.SaveChangesAndLog(gData, handlerResult, cacheService, extensionManager, cancellationToken);
        }

        handlerResult.AddMessage("GlobalData is null", ResultMessageType.Error);
        return handlerResult;
    }

    /// <summary>
    /// Executes a batch of queries and returns a named result set for each.
    /// </summary>
    /// <param name="parameters">Collection of named queries to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of name to query results.</returns>
    public async Task<Dictionary<string, IEnumerable<object>>> MultiQueryAsync(MultiQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var results = new Dictionary<string, IEnumerable<object>>();

        foreach (var query in parameters.Queries)
        {
            var queryResult = await query.ExecuteQuery(dbContext, cancellationToken);
            if (query.Name != null) results.Add(query.Name, queryResult);
        }

        return results;
    }

    /// <summary>
    /// Generic data grid query for any DbSet<T> with dynamic filtering, ordering and paging.
    /// </summary>
    /// <param name="parameters">Grid options including filter, orderBy, skip and take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="T">Entity type which implements ITreeItem.</typeparam>
    /// <returns>Data grid result including total count and items.</returns>
    public async Task<DataGridResult<T>> GetDataGridAsync<T>(DataGridParameters<T> parameters, CancellationToken cancellationToken = default) where T : class, ITreeItem
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();
        
        var result = new DataGridResult<T>();

        // Now you have the DbSet<T> and can query it
        var query = dbContext.Query<T>().AsQueryable();
        
        // Note: Dynamic LINQ string filtering is not supported with RavenDB
        // TODO: Implement proper filtering using expression trees
        // if (!string.IsNullOrEmpty(parameters.Filter))
        // {
        //     query = query.Where(parameters.Filter);
        // }

        // if (!string.IsNullOrEmpty(parameters.OrderBy))
        // {
        //     query = query.OrderBy(parameters.OrderBy);
        // }

        // Important!!! Make sure the Count property of RadzenDataGrid is set.
        result.Count = await query.CountAsync(cancellationToken);

        // Perform paging via Skip and Take.
        result.Items = await query.Skip(parameters.Skip).Take(parameters.Take).ToListAsync(cancellationToken);

        return result;
    }

    private static IRavenQueryable<GlobalData> BuildQuery(GetGlobalDataParameters parameters, IAsyncDocumentSession dbContext)
    {
        return dbContext.
            Query<GlobalData>()
            .Where(x => x.Alias == parameters.Alias);
    }
}