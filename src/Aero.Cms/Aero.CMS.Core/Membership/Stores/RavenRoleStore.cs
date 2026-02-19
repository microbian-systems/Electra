using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aero.CMS.Core.Membership.Models;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Aero.CMS.Core.Membership.Stores;

public class RavenRoleStore : IRoleStore<CmsRole>
{
    private readonly IDocumentStore _documentStore;

    public RavenRoleStore(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public void Dispose()
    {
    }

    public async Task<IdentityResult> CreateAsync(CmsRole role, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        await session.StoreAsync(role, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(CmsRole role, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        await session.StoreAsync(role, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(CmsRole role, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        session.Delete(role.Id.ToString());
        await session.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<string> GetRoleIdAsync(CmsRole role, CancellationToken cancellationToken)
    {
        return role.Id.ToString();
    }

    public async Task<string?> GetRoleNameAsync(CmsRole role, CancellationToken cancellationToken)
    {
        return role.Name;
    }

    public async Task SetRoleNameAsync(CmsRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName ?? string.Empty;
    }

    public async Task<string?> GetNormalizedRoleNameAsync(CmsRole role, CancellationToken cancellationToken)
    {
        return role.NormalizedName;
    }

    public async Task SetNormalizedRoleNameAsync(CmsRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
    }

    public async Task<CmsRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        return await session.LoadAsync<CmsRole>(roleId, cancellationToken);
    }

    public async Task<CmsRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        using var session = _documentStore.OpenAsyncSession();
        return await session.Query<CmsRole>()
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
    }
}
