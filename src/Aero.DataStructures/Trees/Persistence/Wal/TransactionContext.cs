using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Wal;

internal sealed class TransactionContext : ITransactionContext
{
    private readonly long _transactionId;
    private readonly Lsn _beginLsn;
    private readonly IWalWriter _walWriter;
    private readonly TransactionManager _manager;
    private readonly Dictionary<long, ReadOnlyMemory<byte>> _dirtyPages = new();
    private readonly Dictionary<long, Lsn> _writeLsns = new();
    private readonly HashSet<long> _readPages = new();
    private bool _committed;
    private bool _aborted;
    private bool _disposed;

    public long TransactionId => _transactionId;
    public Lsn BeginLsn => _beginLsn;
    public bool IsCommitted => _committed;
    public bool IsAborted => _aborted;
    public IReadOnlyDictionary<long, ReadOnlyMemory<byte>> DirtyPages => _dirtyPages;

    public TransactionContext(
        long transactionId,
        Lsn beginLsn,
        IWalWriter walWriter,
        TransactionManager manager)
    {
        _transactionId = transactionId;
        _beginLsn = beginLsn;
        _walWriter = walWriter;
        _manager = manager;
    }

    public void TrackRead(long pageId)
    {
        ThrowIfDisposed();
        _readPages.Add(pageId);
    }

    public void TrackWrite(long pageId, ReadOnlyMemory<byte> beforeImage)
    {
        ThrowIfDisposed();

        if (!_dirtyPages.ContainsKey(pageId))
        {
            _dirtyPages[pageId] = beforeImage;
        }
    }

    public void RecordWriteLsn(long pageId, Lsn lsn)
    {
        _writeLsns[pageId] = lsn;
    }

    public async ValueTask CommitAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_committed || _aborted)
            throw new InvalidOperationException("Transaction already completed.");

        var commitEntry = new WalEntry
        {
            Header =
            {
                Type = WalEntryType.Commit,
                TransactionId = _transactionId,
            }
        };

        await _walWriter.AppendAsync(commitEntry, ct);
        await _walWriter.FlushAsync(ct);

        _committed = true;
        _manager.Complete(_transactionId);
    }

    public async ValueTask RollbackAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_committed || _aborted)
            throw new InvalidOperationException("Transaction already completed.");

        foreach (var (pageId, beforeImage) in _dirtyPages.Reverse())
        {
            var clrEntry = new WalEntry
            {
                Header =
                {
                    Type = WalEntryType.Clr,
                    TransactionId = _transactionId,
                    PageId = pageId,
                    ReferenceLsn = _writeLsns.GetValueOrDefault(pageId, Lsn.Zero),
                    ImageLength = beforeImage.Length,
                },
                BeforeImage = beforeImage,
                AfterImage = beforeImage,
            };

            await _walWriter.AppendAsync(clrEntry, ct);
        }

        var abortEntry = new WalEntry
        {
            Header =
            {
                Type = WalEntryType.Abort,
                TransactionId = _transactionId,
            }
        };

        await _walWriter.AppendAsync(abortEntry, ct);
        await _walWriter.FlushAsync(ct);

        _aborted = true;
        _manager.Complete(_transactionId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (!_committed && !_aborted)
        {
            await RollbackAsync();
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TransactionContext));
    }
}
