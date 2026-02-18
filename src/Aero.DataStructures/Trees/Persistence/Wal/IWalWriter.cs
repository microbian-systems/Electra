using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public interface IWalWriter : IAsyncDisposable
{
    ValueTask<Lsn> AppendAsync(WalEntry entry, CancellationToken ct = default);
    ValueTask FlushAsync(CancellationToken ct = default);
    Lsn NextLsn { get; }
    long FileSize { get; }
}
