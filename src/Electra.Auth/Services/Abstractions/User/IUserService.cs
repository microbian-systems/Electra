using System.Threading;
using Electra.Models.Entities;

namespace Electra.Auth.Services.Abstractions.User;

public interface IUserService
{
    Task<byte[]> CreateAsync(
        HttpContext httpContext,
        string userName,
        CancellationToken cancellationToken);

    Task<ElectraUser?> FindAsync(
        HttpContext httpContext,
        byte[] userHandle,
        CancellationToken cancellationToken);

    Task<ElectraUser?> FindAsync(
        HttpContext httpContext,
        string userName,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        HttpContext httpContext,
        byte[] userHandle,
        CancellationToken cancellationToken);
}
