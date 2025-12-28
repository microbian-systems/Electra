using Electra.Persistence.Core;
using Electra.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Electra.Persistence;

/// <summary>
/// Interface for Electra-specific unit of work operations
/// </summary>
public interface IElectraUnitOfWork : IAsyncUnitOfWork
{
    public ElectraDbContext Context { get; }
    IApiAuthRepository AuthRepo { get; }
    IUserRepository User { get; }
    IElectraUserProfileRepository userProfileRepo { get; }
}

/// <summary>
/// Unit of Work implementation for ElectraDbContext
/// Handles saving and persisting changes to the database
/// </summary>
public class ElectraUnitOfWork(
    ElectraDbContext context,
    IApiAuthRepository authRepo,
    IElectraUserProfileRepository userProfileRepo,
    IUserRepository userRepository,
    ILogger<ElectraUnitOfWork> log)
    : UnitOfWorkEfCore(context), IElectraUnitOfWork
{
    public new ElectraDbContext Context { get; } = context;

    public IApiAuthRepository AuthRepo { get; } = authRepo;
    public IUserRepository User { get; } = userRepository;
    public IElectraUserProfileRepository userProfileRepo { get; } = userProfileRepo;
}

