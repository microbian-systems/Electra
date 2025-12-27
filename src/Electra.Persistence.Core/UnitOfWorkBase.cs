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
}


public abstract class UnitOfWorkEfCore(DbContext context) : IUnitOfWork, IAsyncUnitOfWork
{
    public DbContext Context { get; } = context;
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    public int SaveChanges()
    {
        return Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}


