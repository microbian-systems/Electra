namespace Aero.CMS.Core.Shared.Interfaces;

/// <summary>
/// Defines a hook that runs before an entity is saved to the repository.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public interface IBeforeSaveHook<in T>
{
    /// <summary>
    /// Gets the execution priority. Lower values run first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Executes the hook logic.
    /// </summary>
    /// <param name="entity">The entity instance being saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(T entity);
}

/// <summary>
/// Defines a hook that runs after an entity has been successfully persisted.
/// </summary>
/// <typeparam name="T">The type of the entity.</typeparam>
public interface IAfterSaveHook<in T>
{
    /// <summary>
    /// Gets the execution priority. Lower values run first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Executes the hook logic.
    /// </summary>
    /// <param name="entity">The entity instance that was saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(T entity);
}
