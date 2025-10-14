using Electra.Auth.Services.Abstractions.User.Models;

namespace Electra.Auth.Services.Abstractions.User;

public interface IUserService
{
    Task<byte[]> CreateAsync(
        HttpContext httpContext,
        string userName,
        CancellationToken cancellationToken);

    Task<ApplicationUser?> FindAsync(
        HttpContext httpContext,
        byte[] userHandle,
        CancellationToken cancellationToken);

    Task<ApplicationUser?> FindAsync(
        HttpContext httpContext,
        string userName,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        HttpContext httpContext,
        byte[] userHandle,
        CancellationToken cancellationToken);
}
