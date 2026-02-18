using System;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Wal;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public sealed class NoIsolationStrategy : IConcurrencyStrategy
{
    public IsolationLevel Level => IsolationLevel.ReadCommitted;

    public ValueTask<IReadSnapshot> BeginReadAsync(long txnId, CancellationToken ct = default)
    {
        return ValueTask.FromResult<IReadSnapshot>(new UnboundedSnapshot(txnId));
    }

    public ValueTask BeginWriteAsync(long txnId, long pageId, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ValidateAsync(ITransactionContext txn, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnCommitAsync(long txnId, Lsn commitLsn, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAbortAsync(long txnId, CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed class UnboundedSnapshot : IReadSnapshot
    {
        private readonly long _txnId;

        public UnboundedSnapshot(long txnId)
        {
            _txnId = txnId;
        }

        public long SnapshotTransactionId => _txnId;
        public bool IsVisible(long xmin, long xmax) => true;
        public void Dispose() { }
    }
}
