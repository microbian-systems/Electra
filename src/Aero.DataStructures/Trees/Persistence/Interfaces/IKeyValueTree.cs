using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Interfaces;

public interface IKeyValueTree<TKey, TValue> : ITree<TKey>
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    ValueTask InsertAsync(TKey key, TValue value, CancellationToken ct = default);
    ValueTask<bool> DeleteAsync(TKey key, CancellationToken ct = default);
    ValueTask<bool> ContainsAsync(TKey key, CancellationToken ct = default);
    ValueTask<(bool found, TValue value)> TryGetAsync(TKey key, CancellationToken ct = default);
    ValueTask<bool> UpdateAsync(TKey key, TValue newValue, CancellationToken ct = default);
    ValueTask<TValue?> FindAsync(TKey key, CancellationToken ct = default);
}

public interface IOrderedKeyValueTree<TKey, TValue> : IKeyValueTree<TKey, TValue>, IOrderedTree<TKey>
    where TKey : unmanaged, IComparable<TKey>
    where TValue : unmanaged
{
    IAsyncEnumerable<(TKey Key, TValue Value)> ScanWithValuesAsync(TKey from, TKey to, CancellationToken ct = default);
    ValueTask<TKey?> FindKeyAsync(TValue value, CancellationToken ct = default);
}
