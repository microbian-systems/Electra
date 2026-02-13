using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Serilog;
using ZauberCMS.Core.Content.Models;
using ZauberCMS.Core.Plugins;
using ZauberCMS.Core.Settings;
using ZauberCMS.Core.Shared.Interfaces;
using ZauberCMS.Core.Shared.Models;
using ZauberCMS.Core.Shared.Services;

namespace ZauberCMS.Core.Extensions;

public static class DocumentSessionExtensions
{
    /// <summary>
    /// Filters the entities based on whether their raw Path column contains the specified contentId.
    /// </summary>
    /// <param name="source">The source IQueryable of entities.</param>
    /// <param name="itemId">The ID to look for in the Path column.</param>
    /// <returns>An IQueryable of entities matching the condition.</returns>
    public static IRavenQueryable<T> WherePathLike<T>(this IRavenQueryable<T> source, string itemId)
        where T : class, IBaseItem
    {
        return source.Where(x => x.Path.Contains(itemId));
    }
    
    public static IRavenQueryable<ContentType> WhereHasCompositionsUsing(this IRavenQueryable<ContentType> source, string itemId)
    {
        return source.Where(x => x.CompositionIds.Contains(itemId));
    }
    
    public static async Task<List<string>> BuildPath<T>(this T entity, IAsyncDocumentSession session, bool isUpdate, IOptions<ZauberSettings> settings)
        where T : class, IBaseItem
    {
        var path = new List<string>();
        var urls = new List<string>();
        IBaseItem? currentEntity = entity;

        while (currentEntity != null)
        {
            path.Insert(0, currentEntity.Id);
            if (currentEntity.Url != null) urls.Insert(0, currentEntity.Url);

            var parentItem = !string.IsNullOrEmpty(currentEntity.ParentId)
                ? await session.LoadAsync<T>(currentEntity.ParentId)
                : null;

            currentEntity = parentItem;
        }

        if (entity is Content.Models.Content)
        {
            if (!isUpdate && settings.Value.EnablePathUrls)
            {
                // New item and path URLs are enabled—generate the URL from the path.
                if (urls.Count > 0) urls.RemoveAt(0); // Remove the root (if applicable)
                entity.Url = string.Join("/", urls);
            }    
        }
        
        return path;
    }
    
    public static IRavenQueryable<T>? ToRavenQueryable<T>(this IAsyncDocumentSession session) where T : class
    {
        try
        {
            return session.Query<T>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Unable to get {nameof(T)} as RavenQueryable");
        }

        return null;
    }
    
    public static async Task<HandlerResult<T>> SaveChangesAndLog<T>(this IAsyncDocumentSession session, T? entity,
        HandlerResult<T> crudResult, ICacheService cacheService, ExtensionManager extensionManager, CancellationToken cancellationToken)
    {
        try
        {
            var canSave = true;
            var entityState = EntityState.Unchanged;
            if (entity != null)
            {
                string? changeVector = null;
                try 
                {
                    changeVector = session.Advanced.GetChangeVectorFor(entity);
                }
                catch
                {
                    // Ignored
                }

                if (changeVector == null)
                {
                    entityState = EntityState.Added;
                }
                else if (session.Advanced.HasChanged(entity))
                {
                    entityState = EntityState.Modified;
                }
            }
            
            // Find any before save plugins
            if (entity != null)
            {
                var beforeSaves = extensionManager.GetInstances<IBeforeEntitySave<T>>(true);
                foreach (var kvp in beforeSaves.OrderBy(x => x.Value.SortOrder))
                {
                    canSave = kvp.Value.BeforeSave(entity, entityState);
                    if (!canSave)
                    {
                        break;
                    }
                }
            }

            if (canSave)
            {
                await session.SaveChangesAsync(cancellationToken);
                
                // Clear cache after successful save
                cacheService.ClearCachedItemsWithPrefix(typeof(T).Name);
                
                crudResult.Success = true;
                if (entity != null)
                {
                    crudResult.Entity = entity;
                }
                
                // After save plugins
                if (entity != null)
                {
                    var afterSaves = extensionManager.GetInstances<IAfterEntitySave<T>>(true);
                    var shouldReSave = false;

                    foreach (var kvp in afterSaves.OrderBy(x => x.Value.SortOrder))
                    {
                        // Use OR operation so if ANY plugin returns true, we re-save
                        // Pass the original entityState since SaveChangesAsync() marks entities as Unchanged
                        shouldReSave |= kvp.Value.AfterSave(entity, entityState);
                    }

                    // If AfterSave logic updated something, persist it to the DB again
                    if (shouldReSave)
                    {
                        await session.SaveChangesAsync(cancellationToken);
                        // Clear cache again after the re-save
                        cacheService.ClearCachedItemsWithPrefix(typeof(T).Name);
                    }
                }

            }
            else
            {
                crudResult.Success = false;
                crudResult.AddMessage("Save was purposely abandoned", ResultMessageType.Error);
            }
        }
        catch (Exception ex)
        {
            crudResult.Success = false;
            crudResult.AddMessage($"{ex.Message} - {ex.InnerException?.Message}", ResultMessageType.Error);
            Log.Error(ex, $"{typeof(T).Name} not saved using SaveChangesAsync");
        }

        return crudResult;
    }


    /// <summary>
    /// Returns paginated list from a queryable
    /// </summary>
    /// <param name="items"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(this IQueryable<T> items, int pageIndex, int pageSize)
    {
        if (items is IRavenQueryable<T> ravenQuery)
        {
            var list = await ravenQuery.Statistics(out var stats)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PaginatedList<T>(list, stats.TotalResults, pageIndex, pageSize);
        }
        
        // Fallback for non-RavenDB queryables (e.g. in-memory lists)
        // Note: This executes synchronously if it's not an async queryable provider, 
        // but since we are in an async method, we can't easily await non-async queryables.
        // However, ToListAsync is an extension for IQueryable in EF Core / RavenDB.
        // If it's a simple List.AsQueryable(), ToListAsync might not be available or behave differently.
        // Assuming this is used in context where async enumeration is possible or it's a standard IQueryable.
        
        var count = items.Count();
        var pagedItems = items.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return new PaginatedList<T>(pagedItems, count, pageIndex, pageSize);
    }
    
    public static PaginatedList<T> ToPaginatedList<T>(this IQueryable<T> query, int pageIndex, int pageSize)
    {
        if (query is IRavenQueryable<T> ravenQuery)
        {
            Raven.Client.Documents.Session.QueryStatistics stats;
            var list = ravenQuery.Statistics(out stats).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<T>(list, stats.TotalResults, pageIndex, pageSize);
        }
        
        var count = query.Count();
        var items = query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}