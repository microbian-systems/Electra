using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Electra.Common.Commands;
using Electra.Models.Entities;

namespace Electra.Persistence.Marten
{
    public interface IDynamicDatabaseQuery<T> : IAsyncCommand<Expression<Func<T, bool>>, IEnumerable<T>> where T : class, IEntity<Guid>
    {
    }
}