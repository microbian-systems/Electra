using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public sealed record RecoveryResult(
    int WinnerTransactions,
    int LoserTransactions,
    int PagesRedone,
    int PagesUndone,
    int EntriesProcessed,
    Lsn RecoveredUpToLsn
);

public sealed class RecoveryEngine
{
    private readonly IStorageBackend _inner;
    private readonly IWalReader _walReader;
    private readonly IWalWriter _walWriter;

    public RecoveryResult? LastRecoveryResult { get; private set; }

    public RecoveryEngine(
        IStorageBackend inner,
        IWalReader walReader,
        IWalWriter walWriter)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _walReader = walReader ?? throw new ArgumentNullException(nameof(walReader));
        _walWriter = walWriter ?? throw new ArgumentNullException(nameof(walWriter));
    }

    public async ValueTask RecoverAsync(Lsn lastCheckpointLsn, CancellationToken ct = default)
    {
        var analysisResult = await AnalysisPhaseAsync(lastCheckpointLsn, ct);
        var redoCount = await RedoPhaseAsync(analysisResult, ct);
        var undoResult = await UndoPhaseAsync(analysisResult, ct);

        LastRecoveryResult = new RecoveryResult(
            analysisResult.WinnerTxns.Count,
            analysisResult.LoserTxns.Count,
            redoCount,
            undoResult.PagesUndone,
            analysisResult.EntriesProcessed,
            analysisResult.MaxLsn
        );
    }

    private async Task<AnalysisResult> AnalysisPhaseAsync(Lsn startLsn, CancellationToken ct)
    {
        var winnerTxns = new HashSet<long>();
        var loserTxns = new HashSet<long>();
        var dirtyPageTable = new Dictionary<long, Lsn>();
        var txnStartLsns = new Dictionary<long, Lsn>();
        var entriesProcessed = 0;
        var maxLsn = startLsn;

        await foreach (var entry in _walReader.ReadFromAsync(startLsn, ct))
        {
            entriesProcessed++;
            maxLsn = entry.Header.Lsn;

            switch (entry.Header.Type)
            {
                case WalEntryType.Begin:
                    loserTxns.Add(entry.Header.TransactionId);
                    txnStartLsns[entry.Header.TransactionId] = entry.Header.Lsn;
                    break;

                case WalEntryType.Commit:
                    loserTxns.Remove(entry.Header.TransactionId);
                    winnerTxns.Add(entry.Header.TransactionId);
                    break;

                case WalEntryType.Abort:
                    loserTxns.Remove(entry.Header.TransactionId);
                    break;

                case WalEntryType.Write:
                    if (!dirtyPageTable.ContainsKey(entry.Header.PageId))
                    {
                        dirtyPageTable[entry.Header.PageId] = entry.Header.Lsn;
                    }
                    break;

                case WalEntryType.Checkpoint:
                    break;
            }
        }

        return new AnalysisResult(
            winnerTxns,
            loserTxns,
            dirtyPageTable,
            txnStartLsns,
            entriesProcessed,
            maxLsn
        );
    }

    private async Task<int> RedoPhaseAsync(AnalysisResult analysis, CancellationToken ct)
    {
        var pagesRedone = 0;

        if (analysis.DirtyPageTable.Count == 0)
            return pagesRedone;

        var minLsn = Lsn.MaxValue;
        foreach (var lsn in analysis.DirtyPageTable.Values)
        {
            if (lsn < minLsn)
                minLsn = lsn;
        }

        await foreach (var entry in _walReader.ReadFromAsync(minLsn, ct))
        {
            ct.ThrowIfCancellationRequested();

            if (entry.Header.Type != WalEntryType.Write)
                continue;

            try
            {
                var pageData = await _inner.ReadPageAsync(entry.Header.PageId, ct);
                var pageLsn = BitConverter.ToUInt64(pageData.Span.Slice(0, 8));

                if (entry.Header.Lsn.Value <= pageLsn)
                    continue;

                var modifiedPage = ApplyAfterImage(pageData, entry);
                await _inner.WritePageAsync(entry.Header.PageId, modifiedPage, ct);
                pagesRedone++;
            }
            catch (PageNotFoundException)
            {
                var newPage = new byte[_inner.PageSize];
                var modifiedPage = ApplyAfterImage(newPage, entry);
                await _inner.WritePageAsync(entry.Header.PageId, modifiedPage, ct);
                pagesRedone++;
            }
        }

        return pagesRedone;
    }

    private async Task<UndoResult> UndoPhaseAsync(AnalysisResult analysis, CancellationToken ct)
    {
        var pagesUndone = 0;
        var writesByTxn = new Dictionary<long, List<WalEntry>>();

        await foreach (var entry in _walReader.ReadAllAsync(ct))
        {
            if (entry.Header.Type == WalEntryType.Write &&
                analysis.LoserTxns.Contains(entry.Header.TransactionId))
            {
                if (!writesByTxn.TryGetValue(entry.Header.TransactionId, out var writes))
                {
                    writes = new List<WalEntry>();
                    writesByTxn[entry.Header.TransactionId] = writes;
                }
                writes.Add(entry);
            }
        }

        foreach (var (txnId, writes) in writesByTxn)
        {
            for (int i = writes.Count - 1; i >= 0; i--)
            {
                var entry = writes[i];

                if (entry.BeforeImage.Length > 0)
                {
                    await _inner.WritePageAsync(entry.Header.PageId, entry.BeforeImage, ct);
                    pagesUndone++;

                    var clrEntry = new WalEntry
                    {
                        Header =
                        {
                            Type = WalEntryType.Clr,
                            TransactionId = txnId,
                            PageId = entry.Header.PageId,
                            ReferenceLsn = entry.Header.Lsn,
                            ImageLength = entry.BeforeImage.Length,
                        },
                        BeforeImage = entry.BeforeImage,
                        AfterImage = entry.BeforeImage,
                    };

                    await _walWriter.AppendAsync(clrEntry, ct);
                }
            }

            var abortEntry = new WalEntry
            {
                Header =
                {
                    Type = WalEntryType.Abort,
                    TransactionId = txnId,
                }
            };

            await _walWriter.AppendAsync(abortEntry, ct);
        }

        await _walWriter.FlushAsync(ct);

        return new UndoResult(pagesUndone);
    }

    private static byte[] ApplyAfterImage(Memory<byte> pageData, WalEntry entry)
    {
        var result = pageData.ToArray();

        if (entry.AfterImage.Length > 0)
        {
            entry.AfterImage.Span.CopyTo(result.AsSpan(entry.Header.PageOffset));
        }

        return result;
    }

    private sealed record AnalysisResult(
        HashSet<long> WinnerTxns,
        HashSet<long> LoserTxns,
        Dictionary<long, Lsn> DirtyPageTable,
        Dictionary<long, Lsn> TxnStartLsns,
        int EntriesProcessed,
        Lsn MaxLsn
    );

    private sealed record UndoResult(int PagesUndone);
}
