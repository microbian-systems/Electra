using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aero.CMS.Core.Membership.Models;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Aero.CMS.Core.Membership.Stores;

public class RavenUserStore : 
    IUserStore<CmsUser>,
    IUserPasswordStore<CmsUser>,
    IUserEmailStore<CmsUser>,
    IUserRoleStore<CmsUser>,
    IUserClaimStore<CmsUser>,
    IUserLockoutStore<CmsUser>
{
    private readonly IDocumentStore _documentStore;

    public RavenUserStore(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public void Dispose()
    {
    }

    #region IUserStore

    public async Task<string> GetUserIdAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.Id.ToString();
    }

    public async Task<string?> GetUserNameAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.UserName;
    }

    public async Task SetUserNameAsync(CmsUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName ?? string.Empty;
    }

    public async Task<string?> GetNormalizedUserNameAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.NormalizedUserName;
    }

    public async Task SetNormalizedUserNameAsync(CmsUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
    }

    public async Task<IdentityResult> CreateAsync(CmsUser user, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        await session.StoreAsync(user, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(CmsUser user, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        await session.StoreAsync(user, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(CmsUser user, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        session.Delete(user.Id.ToString());
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<CmsUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        return await session.LoadAsync<CmsUser>(userId, cancellationToken);
    }

    public async Task<CmsUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        return await session.Query<CmsUser>()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    #endregion

    #region IUserPasswordStore

    public async Task SetPasswordHashAsync(CmsUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
    }

    public async Task<string?> GetPasswordHashAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.PasswordHash;
    }

    public async Task<bool> HasPasswordAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return !string.IsNullOrEmpty(user.PasswordHash);
    }

    #endregion

    #region IUserEmailStore

    public async Task SetEmailAsync(CmsUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email ?? string.Empty;
    }

    public async Task<string?> GetEmailAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.Email;
    }

    public async Task<bool> GetEmailConfirmedAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.EmailConfirmed;
    }

    public async Task SetEmailConfirmedAsync(CmsUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
    }

    public async Task<CmsUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        return await session.Query<CmsUser>()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<string?> GetNormalizedEmailAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.NormalizedEmail;
    }

    public async Task SetNormalizedEmailAsync(CmsUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
    }

    #endregion

    #region IUserRoleStore

    public async Task AddToRoleAsync(CmsUser user, string roleName, CancellationToken cancellationToken)
    {
        if (!user.Roles.Contains(roleName))
        {
            user.Roles.Add(roleName);
        }
    }

    public async Task RemoveFromRoleAsync(CmsUser user, string roleName, CancellationToken cancellationToken)
    {
        user.Roles.Remove(roleName);
    }

    public async Task<IList<string>> GetRolesAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.Roles;
    }

    public async Task<bool> IsInRoleAsync(CmsUser user, string roleName, CancellationToken cancellationToken)
    {
        return user.Roles.Contains(roleName);
    }

    public async Task<IList<CmsUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        return await session.Query<CmsUser>()
            .Where(u => u.Roles.Contains(roleName))
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region IUserClaimStore

    public async Task<IList<Claim>> GetClaimsAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
    }

    public async Task AddClaimsAsync(CmsUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            if (!user.Claims.Any(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value))
            {
                user.Claims.Add(new UserClaim { ClaimType = claim.Type, ClaimValue = claim.Value });
            }
        }
    }

    public async Task ReplaceClaimAsync(CmsUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        var existing = user.Claims.FirstOrDefault(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
        if (existing != null)
        {
            existing.ClaimType = newClaim.Type;
            existing.ClaimValue = newClaim.Value;
        }
    }

    public async Task RemoveClaimsAsync(CmsUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        foreach (var claim in claims)
        {
            var existing = user.Claims.FirstOrDefault(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value);
            if (existing != null)
            {
                user.Claims.Remove(existing);
            }
        }
    }

    public async Task<IList<CmsUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        return await session.Query<CmsUser>()
            .Where(u => u.Claims.Any(c => c.ClaimType == claim.Type && c.ClaimValue == claim.Value))
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region IUserLockoutStore

    public async Task<DateTimeOffset?> GetLockoutEndDateAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.LockoutEnd;
    }

    public async Task SetLockoutEndDateAsync(CmsUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
    }

    public async Task<int> IncrementAccessFailedCountAsync(CmsUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return user.AccessFailedCount;
    }

    public async Task ResetAccessFailedCountAsync(CmsUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
    }

    public async Task<int> GetAccessFailedCountAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.AccessFailedCount;
    }

    public async Task<bool> GetLockoutEnabledAsync(CmsUser user, CancellationToken cancellationToken)
    {
        return user.LockoutEnabled;
    }

    public async Task SetLockoutEnabledAsync(CmsUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
    }

    #endregion
}
