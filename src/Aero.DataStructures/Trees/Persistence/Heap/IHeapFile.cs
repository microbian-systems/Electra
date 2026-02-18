using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Heap;

public interface IHeapFile : IAsyncDisposable
{
    ValueTask<HeapAddress> WriteAsync(
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default);

    ValueTask<Memory<byte>> ReadAsync(
        HeapAddress address,
        CancellationToken ct = default);

    ValueTask DeleteAsync(
        HeapAddress address,
        CancellationToken ct = default);

    ValueTask<HeapAddress> UpdateAsync(
        HeapAddress address,
        ReadOnlyMemory<byte> newData,
        CancellationToken ct = default);

    ValueTask CompactPageAsync(
        long pageId,
        CancellationToken ct = default);

    IAsyncEnumerable<(HeapAddress Address, Memory<byte> Data)> ScanAllAsync(
        CancellationToken ct = default);

    int PageSize { get; }
    long PageCount { get; }
}
