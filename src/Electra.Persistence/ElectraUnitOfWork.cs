using Electra.Persistence.Core;

namespace Electra.Persistence;

/// <summary>
/// Interface for Electra-specific unit of work operations
/// </summary>
public interface IElectraUnitOfWork : IUnitOfWork
{
    public ElectraDbContext Context { get; }
    IApiAuthRepository authReoo { get; }
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
    ILogger<ElectraUnitOfWork> log)
    : UnitOfWorkBase(context), IElectraUnitOfWork
{
    public new ElectraDbContext Context { get; } = context;

    public IApiAuthRepository authReoo { get; } = authRepo;
    public IElectraUserProfileRepository userProfileRepo { get; } = userProfileRepo;
}

