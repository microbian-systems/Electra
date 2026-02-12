using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Stores;

public class UserEmailStore(IAsyncDocumentSession db, IUserStore<CmsUser> userStore) : IUserEmailStore<CmsUser>
{
    private bool disposedValue;

    public async Task<CmsUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await db.Query<CmsUser>()
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<string?> GetEmailAsync(CmsUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(CmsUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task<string?> GetNormalizedEmailAsync(CmsUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetEmailAsync(CmsUser user, string? email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(CmsUser user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(CmsUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            db.Dispose();

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~UserEmailStore()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public Task<string> GetUserIdAsync(CmsUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(CmsUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(CmsUser user, string? userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(CmsUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(CmsUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return userStore.CreateAsync(user, cancellationToken);
    }

    public Task<IdentityResult> UpdateAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return userStore.UpdateAsync(user, cancellationToken);
    }

    public Task<IdentityResult> DeleteAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return userStore.DeleteAsync(user, cancellationToken);
    }

    public Task<CmsUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return userStore.FindByIdAsync(userId, cancellationToken);
    }

    public Task<CmsUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return userStore.FindByNameAsync(normalizedUserName, cancellationToken);
    }
}