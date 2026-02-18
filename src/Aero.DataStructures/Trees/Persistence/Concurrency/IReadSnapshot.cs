using System;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public interface IReadSnapshot : IDisposable
{
    long SnapshotTransactionId { get; }
    bool IsVisible(long xmin, long xmax);
}
