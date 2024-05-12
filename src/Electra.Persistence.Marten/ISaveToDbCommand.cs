using System;
using Electra.Common.Commands;
using Electra.Models.Entities;

namespace Electra.Persistence.Marten
{
    public interface ISaveToDbCommand<T> : IAsyncCommand<T, T>
    {
    }
    
    public interface ISaveToDbCommand<T, TKey> : IAsyncCommand<T, T> where T : IEntity<TKey> where TKey : IEquatable<TKey>
    {
    }
}