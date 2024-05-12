using System;
using Electra.Common.Commands;
using Electra.Core.Entities;

namespace Electra.Persistence.Marten
{
    public interface ISaveToDbCommand<T> : IAsyncCommand<T, T>
    {
    }
    
    public interface ISaveToDbCommand<T, TKey> : IAsyncCommand<T, T> where T : IEntity<TKey> where TKey : IEquatable<TKey>
    {
    }
}