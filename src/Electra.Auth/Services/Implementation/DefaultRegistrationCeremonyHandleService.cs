using System.Threading;
using Electra.Auth.Constants;
using Electra.Auth.Services.Abstractions.CookieStore;
using Electra.Auth.Services.Abstractions.RegistrationCeremonyHandle;
using Microsoft.AspNetCore.DataProtection;

namespace Electra.Auth.Services.Implementation;

public class DefaultRegistrationCeremonyHandleService(IDataProtectionProvider provider)
    : AbstractProtectedCookieStore(provider, DataProtectionPurpose, CookieConstants.RegistrationCeremonyId), IRegistrationCeremonyHandleService
{
    private const string DataProtectionPurpose = "WebAuthn.Net.Demo.RegistrationCeremonyHandle";

    public Task SaveAsync(HttpContext httpContext, string registrationCeremonyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Save(httpContext, Encoding.UTF8.GetBytes(registrationCeremonyId));
        return Task.CompletedTask;
    }

    public Task<string?> ReadAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (TryRead(httpContext, out var registrationCeremonyId))
        {
            return Task.FromResult<string?>(Encoding.UTF8.GetString(registrationCeremonyId));
        }

        return Task.FromResult<string?>(null);
    }

    public Task DeleteAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Delete(httpContext);
        return Task.CompletedTask;
    }
}
