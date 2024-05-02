using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microbians.Common.Commands;
using Microbians.Models.Entities;

namespace Microbians.Persistence.Marten
{
    public interface IDynamicDatabaseCachedQuery<T> : IAsyncCommand<Expression<Func<T, bool>>, IEnumerable<T>> where T : class, IEntity<Guid>
    {
    }
}