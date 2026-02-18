using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees.Persistence.Heap;

public sealed class FreeSpaceMap
{
    private const int Quantum = 32;
    private readonly ConcurrentDictionary<long, int> _freeBytes = new();

    public void Record(long pageId, int freeBytes) =>
        _freeBytes[pageId] = (freeBytes / Quantum) * Quantum;

    public long FindPage(int requiredBytes)
    {
        foreach (var (pageId, free) in _freeBytes)
            if (free >= requiredBytes)
                return pageId;
        return -1;
    }

    public void Remove(long pageId) => _freeBytes.TryRemove(pageId, out _);

    public IEnumerable<long> AllPageIds => _freeBytes.Keys;
}
