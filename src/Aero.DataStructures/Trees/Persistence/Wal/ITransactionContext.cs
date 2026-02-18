using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public interface ITransactionContext : IAsyncDisposable
{
    long TransactionId { get; }
    void TrackRead(long pageId);
    void TrackWrite(long pageId, ReadOnlyMemory<byte> beforeImage);
    ValueTask CommitAsync(CancellationToken ct = default);
    ValueTask RollbackAsync(CancellationToken ct = default);
    bool IsCommitted { get; }
    bool IsAborted { get; }
    IReadOnlyDictionary<long, ReadOnlyMemory<byte>> DirtyPages { get; }
    IReadOnlyDictionary<long, uint> ReadSet { get; }
}
