using Electra.Persistence.Core;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.RavenDB;

public interface IRavenDbUnitOfWork : IAsyncUnitOfWork;

public class RavenDbUnitOfWork(IAsyncDocumentSession session, ILogger<RavenDbUnitOfWork> log) 
    : IRavenDbUnitOfWork
{
    public void Dispose()
    {
        session.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = session.Advanced.WhatChanged();
        var count = changes.Count;
        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to save changes to ravendb");
            return 0;
        }
        return count;
    }
}