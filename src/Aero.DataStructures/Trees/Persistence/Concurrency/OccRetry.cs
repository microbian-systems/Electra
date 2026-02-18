using System;
using System.Threading;
using System.Threading.Tasks;
using Aero.DataStructures.Trees.Persistence.Wal;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public sealed class MaxRetriesExceededException : Exception
{
    public int MaxRetries { get; }

    public MaxRetriesExceededException(int maxRetries)
        : base($"Operation failed after {maxRetries} attempts due to conflicts.")
    {
        MaxRetries = maxRetries;
    }
}

public static class OccRetry
{
    public static async ValueTask<T> ExecuteAsync<T>(
        IWalStorageBackend backend,
        Func<ITransactionContext, ValueTask<T>> operation,
        int maxRetries = 5,
        CancellationToken ct = default)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            await using var txn = await backend.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(txn);
                await txn.CommitAsync(ct);
                return result;
            }
            catch (ConflictException) when (attempt < maxRetries)
            {
                await txn.RollbackAsync(ct);
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt)), ct);
            }
        }
        throw new MaxRetriesExceededException(maxRetries);
    }

    public static async ValueTask ExecuteAsync(
        IWalStorageBackend backend,
        Func<ITransactionContext, ValueTask> operation,
        int maxRetries = 5,
        CancellationToken ct = default)
    {
        await ExecuteAsync<object?>(backend, async txn =>
        {
            await operation(txn);
            return null;
        }, maxRetries, ct);
    }
}
