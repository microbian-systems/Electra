using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Wal;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public sealed class OccConcurrencyStrategy : IConcurrencyStrategy
{
    private readonly ConcurrentDictionary<long, uint> _committedVersions = new();
    private readonly object _validateLock = new();

    public IsolationLevel Level => IsolationLevel.OptimisticOCC;

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
        lock (_validateLock)
        {
            foreach (var (pageId, versionAtRead) in txn.ReadSet)
            {
                if (!_committedVersions.TryGetValue(pageId, out var currentVersion))
                    continue;

                if (currentVersion != versionAtRead)
                    throw new ConflictException(txn.TransactionId, pageId);
            }

            foreach (var pageId in txn.DirtyPages.Keys)
            {
                _committedVersions.AddOrUpdate(
                    pageId,
                    addValue: 1,
                    updateValueFactory: (_, v) => v + 1);
            }
        }

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
