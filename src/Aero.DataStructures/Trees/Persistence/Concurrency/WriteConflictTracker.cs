using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public sealed class WriteConflictTracker
{
    private readonly ConcurrentDictionary<long, List<long>> _pageWriters = new();
    private readonly object _lock = new();

    public void RecordWrite(long pageId, long txnId)
    {
        _pageWriters.GetOrAdd(pageId, _ => new List<long>())
            .Add(txnId);
    }

    public bool HasConflict(long pageId, long readerTxnId)
    {
        if (!_pageWriters.TryGetValue(pageId, out var writers))
            return false;

        return writers.Any(writerTxnId => writerTxnId > readerTxnId);
    }

    public void Evict(long pageId, long safeBeforeTxnId)
    {
        if (!_pageWriters.TryGetValue(pageId, out var writers))
            return;

        lock (_lock)
            writers.RemoveAll(id => id < safeBeforeTxnId);
    }
}
