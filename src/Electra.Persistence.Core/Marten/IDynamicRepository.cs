using System.Linq.Expressions;
using Electra.Core.Entities;

namespace Electra.Persistence.Marten;

// todo - consider placing a constraint on type TKey for the marten repositories
public interface IDynamicMartenRepository : IDynamicRepositoryAsync<string>
{
}

public interface IDynamicReadOnlyRepositoryAsync<TKey> where TKey : IEquatable<TKey>
{
    Task InvalidateCacheAsync<T>(IEnumerable<T> documents) where T : class, IEntity<TKey>, new();
    Task<long> CountAsync<T>() where T : class, IEntity<TKey>, new();
    Task<T> GetByIdAsync<T>(TKey id) where T : class, IEntity<TKey>, new();
    Task<IReadOnlyCollection<T>> GetByIdsAsync<T>(List<TKey> ids) where T : class, IEntity<TKey>, new();
    Task<IEnumerable<T>> GetAllAsync<T>() where T : class, IEntity<TKey>, new();
    Task<bool> ExistsAsync<T>(TKey id) where T : class, IEntity<TKey>, new();
        
    Task<IEnumerable<T>> Search<T>(Expression<Func<T, bool>> predicate) where T : class, IEntity<TKey>, new();

    Task<T> FindSingle<T>(Expression<Func<T, bool>> predicate) where T : class, IEntity<TKey>, new();
}
    
public interface IDynamicRepositoryAsync<TKey> : IDynamicReadOnlyRepositoryAsync<TKey> where TKey : IEquatable<TKey>
{
    Task<T> SaveAsync<T>(T document) where T : class, IEntity<TKey>, new();
    Task SaveAsync<T>(IEnumerable<T> documents) where T : class, IEntity<TKey>, new();
    Task DeleteAsync<T>(TKey id) where T : class, IEntity<TKey>, new();
    Task DeleteAsync<T>(List<TKey> ids) where T : class, IEntity<TKey>, new();
    Task DeleteAsync<T>(T document) where T : class, IEntity<TKey>, new();
    Task DeleteAsync<T>(IEnumerable<T> documents) where T : class, IEntity<TKey>, new();
    //Task<long> DeleteAllAsync<T>();
        
    #region "unused from Foundatio.Repositories
// todo - impl AsyncEvents like Foundatio.Repositories e.g. below
//        AsyncEvent<BeforeQueryEventArgs<T>> BeforeQuery { get; set; }
//        AsyncEvent<DocumentsEventArgs<T>> DocumentsAdding { get; set; }
//        AsyncEvent<DocumentsEventArgs<T>> DocumentsAdded { get; set; }
//        AsyncEvent<ModifiedDocumentsEventArgs<T>> DocumentsSaving { get; set; }
//        AsyncEvent<ModifiedDocumentsEventArgs<T>> DocumentsSaved { get; set; }
//        AsyncEvent<DocumentsEventArgs<T>> DocumentsRemoving { get; set; }
//        AsyncEvent<DocumentsEventArgs<T>> DocumentsRemoved { get; set; }
//        AsyncEvent<DocumentsChangeEventArgs<T>> DocumentsChanging { get; set; }
//        AsyncEvent<DocumentsChangeEventArgs<T>> DocumentsChanged { get; set; }
    #endregion
}