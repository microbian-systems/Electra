using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public sealed class CheckpointService : BackgroundService
{
    private readonly IWalStorageBackend _walBackend;
    private readonly IWalWriter _walWriter;
    private readonly IWalReader _walReader;
    private readonly IStorageBackend _innerBackend;
    private readonly TransactionManager _txnManager;
    private readonly CheckpointOptions _options;
    private readonly ILogger<CheckpointService> _logger;
    private int _entriesSinceLastCheckpoint;

    public CheckpointService(
        IWalStorageBackend walBackend,
        IWalWriter walWriter,
        IWalReader walReader,
        IStorageBackend innerBackend,
        TransactionManager txnManager,
        IOptions<CheckpointOptions> options,
        ILogger<CheckpointService> logger)
    {
        _walBackend = walBackend ?? throw new ArgumentNullException(nameof(walBackend));
        _walWriter = walWriter ?? throw new ArgumentNullException(nameof(walWriter));
        _walReader = walReader ?? throw new ArgumentNullException(nameof(walReader));
        _innerBackend = innerBackend ?? throw new ArgumentNullException(nameof(innerBackend));
        _txnManager = txnManager ?? throw new ArgumentNullException(nameof(txnManager));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "CheckpointService started with check interval {Interval}",
            _options.CheckInterval);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.CheckInterval, ct);

                var shouldCheckpoint = _walWriter.FileSize >= _options.WalSizeThresholdBytes ||
                    _entriesSinceLastCheckpoint >= _options.WalEntryCountThreshold;

                if (!shouldCheckpoint)
                {
                    _logger.LogDebug(
                        "WAL size {Size} and entry count {Count} below thresholds",
                        _walWriter.FileSize,
                        _entriesSinceLastCheckpoint);
                    continue;
                }

                await CheckpointAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkpoint execution");
            }
        }

        if (_options.CheckpointOnShutdown && !ct.IsCancellationRequested)
        {
            try
            {
                await CheckpointAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during shutdown checkpoint");
            }
        }

        _logger.LogInformation("CheckpointService stopped");
    }

    public async Task CheckpointAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting checkpoint");

        var activeTxns = _txnManager.ActiveTransactionStartLsns.ToList();
        var safeCheckpointLsn = activeTxns.Count > 0
            ? activeTxns.Min()
            : new Lsn(_walWriter.NextLsn.Value > 0 ? _walWriter.NextLsn.Value - 1 : 0);

        _logger.LogDebug(
            "Safe checkpoint LSN: {Lsn}, active transactions: {Count}",
            safeCheckpointLsn,
            activeTxns.Count);

        var committedTxns = new HashSet<long>();

        await foreach (var entry in _walReader.ReadFromAsync(Lsn.MinValue, ct))
        {
            if (entry.Header.Lsn > safeCheckpointLsn)
                break;

            if (entry.Header.Type == WalEntryType.Commit)
            {
                committedTxns.Add(entry.Header.TransactionId);
            }
        }

        var pagesToFlush = new Dictionary<long, Lsn>();

        await foreach (var entry in _walReader.ReadFromAsync(Lsn.MinValue, ct))
        {
            if (entry.Header.Lsn > safeCheckpointLsn)
                break;

            if (entry.Header.Type == WalEntryType.Write &&
                committedTxns.Contains(entry.Header.TransactionId))
            {
                if (!pagesToFlush.ContainsKey(entry.Header.PageId))
                {
                    pagesToFlush[entry.Header.PageId] = entry.Header.Lsn;
                }
            }
        }

        foreach (var (pageId, entryLsn) in pagesToFlush)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var pageData = await _innerBackend.ReadPageAsync(pageId, ct);
                var pageLsn = BitConverter.ToUInt64(pageData.Span.Slice(0, 8));

                if (entryLsn.Value <= pageLsn)
                    continue;

                await foreach (var entry in _walReader.ReadFromAsync(entryLsn, ct))
                {
                    if (entry.Header.Lsn != entryLsn)
                        continue;

                    if (entry.Header.PageId != pageId)
                        continue;

                    if (entry.AfterImage.Length > 0)
                    {
                        var modifiedPage = pageData.ToArray();
                        entry.AfterImage.Span.CopyTo(modifiedPage.AsSpan(entry.Header.PageOffset));
                        await _innerBackend.WritePageAsync(pageId, modifiedPage, ct);
                    }
                    break;
                }
            }
            catch (PageNotFoundException)
            {
            }
        }

        await _innerBackend.FlushAsync(ct);

        var checkpointEntry = new WalEntry
        {
            Header =
            {
                Type = WalEntryType.Checkpoint,
                ReferenceLsn = safeCheckpointLsn,
            }
        };

        await _walWriter.AppendAsync(checkpointEntry, ct);
        await _walWriter.FlushAsync(ct);

        if (_walWriter is WalFile walFile)
        {
            await walFile.TruncateBeforeAsync(safeCheckpointLsn, ct);
        }

        _entriesSinceLastCheckpoint = 0;

        _logger.LogInformation(
            "Checkpoint completed at LSN {Lsn}, {Pages} pages flushed",
            safeCheckpointLsn,
            pagesToFlush.Count);
    }

    public void RecordEntry()
    {
        Interlocked.Increment(ref _entriesSinceLastCheckpoint);
    }
}
