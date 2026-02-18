using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Wal;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public sealed class MvccConcurrencyStrategy : IConcurrencyStrategy
{
    private readonly CommitTable _commitTable = new();
    private readonly WriteConflictTracker _writeConflictTracker = new();
    private readonly ConcurrentDictionary<long, long> _activeTxns = new();

    public IsolationLevel Level => IsolationLevel.SnapshotMVCC;

    public ValueTask<IReadSnapshot> BeginReadAsync(long txnId, CancellationToken ct = default)
    {
        var inProgress = new HashSet<long>(_activeTxns.Keys);
        inProgress.Remove(txnId);
        var snapshot = new MvccSnapshot(txnId, _commitTable, inProgress);
        return ValueTask.FromResult<IReadSnapshot>(snapshot);
    }

    public ValueTask BeginWriteAsync(long txnId, long pageId, CancellationToken ct = default)
    {
        _activeTxns.TryAdd(txnId, txnId);
        return ValueTask.CompletedTask;
    }

    public ValueTask ValidateAsync(ITransactionContext txn, CancellationToken ct = default)
    {
        foreach (var pageId in txn.DirtyPages.Keys)
        {
            if (_writeConflictTracker.HasConflict(pageId, txn.TransactionId))
                throw new ConflictException(txn.TransactionId, pageId);
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask OnCommitAsync(long txnId, Lsn commitLsn, CancellationToken ct = default)
    {
        _commitTable.RecordCommit(txnId);
        _activeTxns.TryRemove(txnId, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAbortAsync(long txnId, CancellationToken ct = default)
    {
        _commitTable.RecordAbort(txnId);
        _activeTxns.TryRemove(txnId, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
