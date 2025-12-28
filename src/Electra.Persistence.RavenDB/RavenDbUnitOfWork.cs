using Electra.Persistence.Core;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence.RavenDB;

public interface IRavenDbUnitOfWork : IAsyncUnitOfWork
{
    // Add your repository property here
    IElectraUserRepository Users { get; }
}


public class RavenDbUnitOfWork : IRavenDbUnitOfWork
{
    private readonly IAsyncDocumentSession _session;
    private readonly ILogger<RavenDbUnitOfWork> _log;
    private readonly ILoggerFactory _loggerFactory;

    // Backing field for the repository
    private IElectraUserRepository? _users;

    public RavenDbUnitOfWork(
        IAsyncDocumentSession session, 
        ILogger<RavenDbUnitOfWork> log,
        ILoggerFactory loggerFactory) 
    {
        _session = session;
        _log = log;
        _loggerFactory = loggerFactory;
    }

    // Lazy initialization ensures the repo is only created when accessed
    // and guarantees it uses the UoW's specific session.
    public IElectraUserRepository Users
    {
        get
        {
            if (_users == null)
            {
                var repoLogger = _loggerFactory.CreateLogger<ElectraUserRepository>();
                _users = new ElectraUserRepository(_session, repoLogger);
            }
            return _users;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Your existing logic
        var changes = _session.Advanced.WhatChanged();
        var count = changes.Count;
        
        try
        {
            await _session.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to save changes to RavenDB");
            return 0; // Or re-throw, depending on your error handling strategy
        }

        return count;
    }

    public Task StartTransactionAsync(CancellationToken cancellationToken = default)
    {
        // RavenDB sessions are transactional by default. 
        // We could use ClusterTransaction if needed, but for standard session-level transactions,
        // just having the session is enough.
        return Task.CompletedTask;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(cancellationToken);
    }

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        // To rollback in RavenDB session, we clear the session state.
        _session.Advanced.Clear();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _session.Dispose();
        GC.SuppressFinalize(this);
    }
}