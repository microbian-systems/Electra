using System;
using Microbians.Common.Commands;
using Microbians.Models.Entities;

namespace Microbians.Persistence.Marten
{
    public interface ISaveToDbCommand<T> : IAsyncCommand<T, T>
    {
    }
    
    public interface ISaveToDbCommand<T, TKey> : IAsyncCommand<T, T> where T : IEntity<TKey> where TKey : IEquatable<TKey>
    {
    }
}