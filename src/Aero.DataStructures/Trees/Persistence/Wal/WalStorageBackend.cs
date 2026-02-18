using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public sealed class WalStorageBackend : IWalStorageBackend
{
    private readonly IStorageBackend _inner;
    private readonly IWalWriter _walWriter;
    private readonly TransactionManager _txnManager;
    private readonly Dictionary<long, byte[]> _dirtyPageCache = new();
    private TransactionContext? _currentTxn;
    private Lsn _lastCommittedLsn = Lsn.Zero;
    private bool _disposed;

    public int PageSize => _inner.PageSize;
    public long PageCount => _inner.PageCount;
    public Lsn LastCommittedLsn => _lastCommittedLsn;

    public WalStorageBackend(
        IStorageBackend inner,
        IWalWriter walWriter,
        TransactionManager txnManager)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _walWriter = walWriter ?? throw new ArgumentNullException(nameof(walWriter));
        _txnManager = txnManager ?? throw new ArgumentNullException(nameof(txnManager));
    }

    public ValueTask<ITransactionContext> BeginTransactionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_currentTxn is { IsCommitted: false, IsAborted: false })
            throw new InvalidOperationException("A transaction is already active.");

        var txn = _txnManager.BeginAsync(ct).GetAwaiter().GetResult();
        _currentTxn = (TransactionContext)txn;
        return new ValueTask<ITransactionContext>(txn);
    }

    public async ValueTask<Memory<byte>> ReadPageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_dirtyPageCache.TryGetValue(pageId, out var cached))
        {
            return cached;
        }

        return await _inner.ReadPageAsync(pageId, ct);
    }

    public async ValueTask WritePageAsync(long pageId, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_currentTxn == null || _currentTxn.IsCommitted || _currentTxn.IsAborted)
            throw new InvalidOperationException("No active transaction. Call BeginTransactionAsync first.");

        Memory<byte> beforeImage;
        try
        {
            beforeImage = await _inner.ReadPageAsync(pageId, ct);
        }
        catch (PageNotFoundException)
        {
            beforeImage = new byte[_inner.PageSize];
        }

        var beforeBytes = beforeImage.ToArray();

        var pageLsn = _walWriter.NextLsn;
        var dataBytes = data.ToArray();
        BinaryPrimitives.WriteUInt64LittleEndian(dataBytes, pageLsn.Value);

        var writeEntry = new WalEntry
        {
            Header =
            {
                Type = WalEntryType.Write,
                TransactionId = _currentTxn.TransactionId,
                PageId = pageId,
                PageOffset = 0,
                ImageLength = dataBytes.Length,
            },
            BeforeImage = beforeBytes,
            AfterImage = dataBytes,
        };

        var lsn = await _walWriter.AppendAsync(writeEntry, ct);
        _currentTxn.RecordWriteLsn(pageId, lsn);
        _currentTxn.TrackWrite(pageId, beforeBytes);

        await _inner.WritePageAsync(pageId, dataBytes, ct);
        _dirtyPageCache[pageId] = dataBytes;
    }

    public async ValueTask<long> AllocatePageAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_currentTxn == null || _currentTxn.IsCommitted || _currentTxn.IsAborted)
            throw new InvalidOperationException("No active transaction. Call BeginTransactionAsync first.");

        var newPageId = await _inner.AllocatePageAsync(ct);

        var allocEntry = new WalEntry
        {
            Header =
            {
                Type = WalEntryType.Allocate,
                TransactionId = _currentTxn.TransactionId,
                PageId = newPageId,
            }
        };

        await _walWriter.AppendAsync(allocEntry, ct);

        return newPageId;
    }

    public async ValueTask FreePageAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (_currentTxn == null || _currentTxn.IsCommitted || _currentTxn.IsAborted)
            throw new InvalidOperationException("No active transaction. Call BeginTransactionAsync first.");

        var freeEntry = new WalEntry
        {
            Header =
            {
                Type = WalEntryType.Free,
                TransactionId = _currentTxn.TransactionId,
                PageId = pageId,
            }
        };

        await _walWriter.AppendAsync(freeEntry, ct);
        await _inner.FreePageAsync(pageId, ct);
        _dirtyPageCache.Remove(pageId);
    }

    public async ValueTask<PageMetadata> GetPageMetadataAsync(long pageId, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await _inner.GetPageMetadataAsync(pageId, ct);
    }

    public async ValueTask UpdatePageMetadataAsync(
        long pageId,
        int liveDelta,
        int deadDelta,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _inner.UpdatePageMetadataAsync(pageId, liveDelta, deadDelta, ct);
    }

    public async IAsyncEnumerable<PageMetadata> GetFragmentedPagesAsync(
        double threshold,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await foreach (var meta in _inner.GetFragmentedPagesAsync(threshold, ct))
        {
            yield return meta;
        }
    }

    public async ValueTask FlushAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _inner.FlushAsync(ct);
    }

    internal async ValueTask OnTransactionCommittedAsync(Lsn commitLsn)
    {
        _lastCommittedLsn = commitLsn;
        _dirtyPageCache.Clear();
        _currentTxn = null;
    }

    internal async ValueTask OnTransactionRolledBackAsync()
    {
        foreach (var (pageId, beforeImage) in _currentTxn!.DirtyPages)
        {
            await _inner.WritePageAsync(pageId, beforeImage.ToArray());
        }

        _dirtyPageCache.Clear();
        _currentTxn = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_currentTxn is { IsCommitted: false, IsAborted: false })
        {
            await _currentTxn.RollbackAsync();
        }

        await _walWriter.DisposeAsync();
        await _inner.DisposeAsync();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WalStorageBackend));
    }
}
