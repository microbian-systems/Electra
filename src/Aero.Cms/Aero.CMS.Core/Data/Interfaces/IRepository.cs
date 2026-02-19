using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;

namespace Aero.CMS.Core.Data.Interfaces;

public interface IRepository<T> where T : class, IEntity<Guid>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<HandlerResult> SaveAsync(T entity, CancellationToken ct = default);
    Task<HandlerResult> DeleteAsync(Guid id, CancellationToken ct = default);
}
