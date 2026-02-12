using System;
using System.Threading;
using System.Threading.Tasks;

namespace Electra.Persistence.Core;

public interface IUnitOfWork : IDisposable
{
    public int SaveChanges();
}

public interface IAsyncUnitOfWork : IDisposable
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    public Task StartTransactionAsync(CancellationToken cancellationToken = default);
    public Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}