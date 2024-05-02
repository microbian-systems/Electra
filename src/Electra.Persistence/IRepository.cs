namespace Electra.Persistence
{
    public interface IRepository<T, Tkey> : IReadOnlyRepository<T, Tkey> where T : IEntity<Tkey> where Tkey : IEquatable<Tkey>
    {
        Task<T> AddAsync(T entity);
        Task AddAsync(IEnumerable<T> entities);
        Task<long> RemoveAllAsync();
        Task RemoveAsync(IEnumerable<Tkey> ids);
        Task RemoveAsync(Tkey id);
        Task RemoveAsync(T entity);
        Task RemoveAsync(IEnumerable<T> entities);
        Task SaveAsync(IEnumerable<T> entities);
        Task<T> SaveAsync(T entity);
    }
}