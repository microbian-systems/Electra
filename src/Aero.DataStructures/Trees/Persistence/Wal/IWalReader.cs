using System;
using System.Collections.Generic;
using System.Threading;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public interface IWalReader : IAsyncDisposable
{
    IAsyncEnumerable<WalEntry> ReadFromAsync(Lsn startLsn, CancellationToken ct = default);
    IAsyncEnumerable<WalEntry> ReadAllAsync(CancellationToken ct = default);
}
