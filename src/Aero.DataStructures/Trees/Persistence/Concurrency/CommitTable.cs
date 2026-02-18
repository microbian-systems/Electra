using System.Collections.Concurrent;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public sealed class CommitTable
{
    private readonly ConcurrentDictionary<long, CommitStatus> _entries = new();

    public void RecordCommit(long txnId) =>
        _entries[txnId] = new CommitStatus(txnId, CommitState.Committed);

    public void RecordAbort(long txnId) =>
        _entries[txnId] = new CommitStatus(txnId, CommitState.Aborted);

    public CommitState GetState(long txnId)
    {
        if (txnId == 0) return CommitState.Committed;
        return _entries.TryGetValue(txnId, out var status)
            ? status.State
            : CommitState.InProgress;
    }

    public bool IsCommitted(long txnId) => GetState(txnId) == CommitState.Committed;
    public bool IsAborted(long txnId) => GetState(txnId) == CommitState.Aborted;
    public bool IsInProgress(long txnId) => GetState(txnId) == CommitState.InProgress;

    public void Evict(long txnId) => _entries.TryRemove(txnId, out _);
}

public enum CommitState : byte
{
    InProgress = 0,
    Committed = 1,
    Aborted = 2,
}

public readonly record struct CommitStatus(long TxnId, CommitState State);
