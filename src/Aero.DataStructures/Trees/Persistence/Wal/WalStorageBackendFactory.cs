using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Concurrency;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public static class WalStorageBackendFactory
{
    public static async ValueTask<IWalStorageBackend> CreateAsync(
        IStorageBackend inner,
        string walPath,
        IConcurrencyStrategy? concurrency = null,
        CancellationToken ct = default)
    {
        var walFile = new WalFile(walPath);
        var txnManager = new TransactionManager(walFile);

        if (walFile.FileSize > WalFile.HeaderSize)
        {
            var recovery = new RecoveryEngine(inner, walFile, walFile);
            await recovery.RecoverAsync(walFile.LastCheckpointLsn, ct);
        }

        return new WalStorageBackend(inner, walFile, txnManager, concurrency);
    }

    public static ValueTask<IWalStorageBackend> CreateWithMvccAsync(
        IStorageBackend inner,
        string walPath,
        CancellationToken ct = default) =>
        CreateAsync(inner, walPath, new MvccConcurrencyStrategy(), ct);

    public static ValueTask<IWalStorageBackend> CreateWithOccAsync(
        IStorageBackend inner,
        string walPath,
        CancellationToken ct = default) =>
        CreateAsync(inner, walPath, new OccConcurrencyStrategy(), ct);
}
