using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public sealed class MvccSnapshot : IReadSnapshot
{
    private readonly long _snapshotTxnId;
    private readonly CommitTable _commitTable;
    private readonly IReadOnlySet<long> _inProgressAtSnapshot;

    public MvccSnapshot(
        long snapshotTxnId,
        CommitTable commitTable,
        IReadOnlySet<long> inProgressAtSnapshot)
    {
        _snapshotTxnId = snapshotTxnId;
        _commitTable = commitTable;
        _inProgressAtSnapshot = inProgressAtSnapshot;
    }

    public long SnapshotTransactionId => _snapshotTxnId;

    public bool IsVisible(long xmin, long xmax)
    {
        if (!_commitTable.IsCommitted(xmin)) return false;
        if (_inProgressAtSnapshot.Contains(xmin)) return false;

        if (xmax == 0) return true;

        if (_inProgressAtSnapshot.Contains(xmax)) return true;
        if (!_commitTable.IsCommitted(xmax)) return true;

        return false;
    }

    public void Dispose() { }
}
