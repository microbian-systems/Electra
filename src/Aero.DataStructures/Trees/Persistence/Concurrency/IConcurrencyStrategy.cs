using System;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Wal;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public interface IConcurrencyStrategy : IAsyncDisposable
{
    ValueTask<IReadSnapshot> BeginReadAsync(long transactionId, CancellationToken ct = default);
    ValueTask BeginWriteAsync(long transactionId, long pageId, CancellationToken ct = default);
    ValueTask ValidateAsync(ITransactionContext txn, CancellationToken ct = default);
    ValueTask OnCommitAsync(long transactionId, Lsn commitLsn, CancellationToken ct = default);
    ValueTask OnAbortAsync(long transactionId, CancellationToken ct = default);
    IsolationLevel Level { get; }
}
