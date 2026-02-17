using Aero.Common.Commands;
using Aero.Core.Entities;


namespace Aero.Marten;

public interface IDynamicDbCachedQuery<T> 
    : IAsyncCommand<Expression<Func<T, bool>>, IEnumerable<T>> 
    where T : class, IEntity<Guid>
{
}