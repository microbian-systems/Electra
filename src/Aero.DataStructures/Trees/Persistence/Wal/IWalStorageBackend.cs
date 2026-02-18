using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public interface IWalStorageBackend : IStorageBackend
{
    ValueTask<ITransactionContext> BeginTransactionAsync(CancellationToken ct = default);
    Lsn LastCommittedLsn { get; }
}
