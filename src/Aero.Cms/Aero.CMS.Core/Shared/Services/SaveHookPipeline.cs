using Aero.CMS.Core.Shared.Interfaces;

namespace Aero.CMS.Core.Shared.Services;

/// <summary>
/// Pipeline for executing save hooks for a specific entity type.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public class SaveHookPipeline<T>
{
    private readonly IEnumerable<IBeforeSaveHook<T>> _beforeHooks;
    private readonly IEnumerable<IAfterSaveHook<T>> _afterHooks;

    public SaveHookPipeline(
        IEnumerable<IBeforeSaveHook<T>> beforeHooks,
        IEnumerable<IAfterSaveHook<T>> afterHooks)
    {
        _beforeHooks = beforeHooks.OrderBy(h => h.Priority);
        _afterHooks = afterHooks.OrderBy(h => h.Priority);
    }

    /// <summary>
    /// Runs all registered before-save hooks in priority order.
    /// </summary>
    /// <param name="entity">The entity instance being saved.</param>
    public async Task RunBeforeAsync(T entity)
    {
        foreach (var hook in _beforeHooks)
        {
            await hook.ExecuteAsync(entity);
        }
    }

    /// <summary>
    /// Runs all registered after-save hooks in priority order.
    /// </summary>
    /// <param name="entity">The entity instance that was saved.</param>
    public async Task RunAfterAsync(T entity)
    {
        foreach (var hook in _afterHooks)
        {
            await hook.ExecuteAsync(entity);
        }
    }
}
