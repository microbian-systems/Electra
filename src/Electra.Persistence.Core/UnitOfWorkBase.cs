using Microsoft.EntityFrameworkCore;

namespace Electra.Persistence.Core;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    void SaveChanges();
}

public abstract class UnitOfWorkBase(DbContext context) : IUnitOfWork
{
    public DbContext Context { get; } = context;
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }

    public void SaveChanges()
    {
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}


