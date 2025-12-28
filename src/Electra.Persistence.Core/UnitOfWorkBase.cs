using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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


public abstract class UnitOfWorkEfCore(DbContext context) : IUnitOfWork, IAsyncUnitOfWork
{
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;
    public DbContext Context { get; } = context;
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    public int SaveChanges()
    {
        return Context.SaveChanges();
    }

    public async Task StartTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}


