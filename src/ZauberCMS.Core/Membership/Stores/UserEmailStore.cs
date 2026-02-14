using Electra.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Stores;

public class UserEmailStore(IAsyncDocumentSession db, IUserStore<ElectraUser> userStore) : IUserEmailStore<ElectraUser>
{
    private bool disposedValue;

    public async Task<ElectraUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await db.Query<ElectraUser>()
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<string?> GetEmailAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.EmailConfirmed);
    }

    public Task<string?> GetNormalizedEmailAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetEmailAsync(ElectraUser user, string? email, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(ElectraUser user, bool confirmed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(ElectraUser user, string? normalizedEmail, CancellationToken cancellationToken)
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

    public Task<string> GetUserIdAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.Id.ToString());
    }

    public Task<string?> GetUserNameAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(ElectraUser user, string? userName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task SetNormalizedUserNameAsync(ElectraUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> CreateAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        return userStore.CreateAsync(user, cancellationToken);
    }

    public Task<IdentityResult> UpdateAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        return userStore.UpdateAsync(user, cancellationToken);
    }

    public Task<IdentityResult> DeleteAsync(ElectraUser user, CancellationToken cancellationToken)
    {
        return userStore.DeleteAsync(user, cancellationToken);
    }

    public Task<ElectraUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return userStore.FindByIdAsync(userId, cancellationToken);
    }

    public Task<ElectraUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return userStore.FindByNameAsync(normalizedUserName, cancellationToken);
    }
}