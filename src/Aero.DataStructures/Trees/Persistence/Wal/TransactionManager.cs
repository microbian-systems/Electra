using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public sealed class TransactionManager : IAsyncDisposable
{
    private readonly IWalWriter _wal;
    private long _nextTransactionId;
    private readonly ConcurrentDictionary<long, ITransactionContext> _active = new();
    private bool _disposed;

    public TransactionManager(IWalWriter wal)
    {
        _wal = wal ?? throw new ArgumentNullException(nameof(wal));
        _nextTransactionId = 0;
    }

    public async ValueTask<ITransactionContext> BeginAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var txnId = Interlocked.Increment(ref _nextTransactionId);

        var beginEntry = new WalEntry
        {
            Header =
            {
                Type = WalEntryType.Begin,
                TransactionId = txnId,
            }
        };

        var lsn = await _wal.AppendAsync(beginEntry, ct);

        var ctx = new TransactionContext(txnId, lsn, _wal, this);
        _active[txnId] = ctx;

        return ctx;
    }

    internal void Complete(long transactionId)
    {
        _active.TryRemove(transactionId, out _);
    }

    public IEnumerable<Lsn> ActiveTransactionStartLsns =>
        _active.Values
            .Select(t => ((TransactionContext)t).BeginLsn)
            .OrderBy(l => l);

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var txn in _active.Values.ToArray())
        {
            await txn.RollbackAsync();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TransactionManager));
    }
}
