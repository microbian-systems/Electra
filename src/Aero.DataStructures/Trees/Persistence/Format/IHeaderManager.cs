using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Format;

namespace Aero.DataStructures.Trees.Persistence.Format;

public interface IHeaderManager
{
    ValueTask<FileHeader> ReadAsync(CancellationToken ct = default);
    ValueTask WriteAsync(FileHeader header, CancellationToken ct = default);
    ValueTask PersistNextTransactionIdAsync(long value, CancellationToken ct = default);
    ValueTask PersistMinActiveTxnIdAsync(long value, CancellationToken ct = default);
    ValueTask SetShutdownStateAsync(ShutdownState state, CancellationToken ct = default);
}
