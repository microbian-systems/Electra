using Electra.Models.Entities;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Membership.Interfaces;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Services;

/// <summary>
/// Service for managing CMS user profiles and role UI data with RavenDB.
/// Uses Include() patterns for efficient document loading.
/// </summary>
public class ElectraUserProfileService(IAsyncDocumentSession db) : IElectraUserProfileService
{
    /// <summary>
    /// Gets a user profile by user ID
    /// </summary>
    public async Task<CmsUserProfile?> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await db.Query<CmsUserProfile>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Gets a user profile with the associated user loaded via Include()
    /// </summary>
    public async Task<(ElectraUser? User, CmsUserProfile? Profile)> GetUserWithProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Load the profile
        var profile = await db.Query<CmsUserProfile>()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (profile == null)
            return (null, null);

        // Load the user separately
        var user = await db.LoadAsync<ElectraUser>(userId, cancellationToken);
        profile.User = user;

        return (user, profile);
    }

    /// <summary>
    /// Gets or creates a user profile for the specified user
    /// </summary>
    public async Task<CmsUserProfile> GetOrCreateProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(userId, cancellationToken);

        if (profile == null)
        {
            profile = new CmsUserProfile
            {
                UserId = userId,
                CreatedOn = DateTime.UtcNow,
                PropertyData = [],
                ExtendedData = new Dictionary<string, object>()
            };
            await db.StoreAsync(profile, cancellationToken);
        }

        return profile;
    }

    /// <summary>
    /// Saves a user profile
    /// </summary>
    public async Task<CmsUserProfile> SaveProfileAsync(CmsUserProfile profile, CancellationToken cancellationToken = default)
    {
        profile.ModifiedOn = DateTime.UtcNow;
        await db.StoreAsync(profile, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    /// <summary>
    /// Gets role UI data by role ID
    /// </summary>
    public async Task<CmsRoleUI?> GetRoleUIAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return await db.Query<CmsRoleUI>()
            .FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);
    }

    /// <summary>
    /// Gets role UI data with the associated role loaded via Include()
    /// </summary>
    public async Task<(CmsRole? Role, CmsRoleUI? UI)> GetRoleWithUIAsync(string roleId, CancellationToken cancellationToken = default)
    {
        // Load the role UI
        var roleUI = await db.Query<CmsRoleUI>()
            .FirstOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);

        if (roleUI == null)
            return (null, null);

        // Load the role separately
        var role = await db.LoadAsync<CmsRole>(roleId, cancellationToken);
        roleUI.Role = role;

        return (role, roleUI);
    }

    /// <summary>
    /// Gets or creates role UI data for the specified role
    /// </summary>
    public async Task<CmsRoleUI> GetOrCreateRoleUIAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var roleUI = await GetRoleUIAsync(roleId, cancellationToken);

        if (roleUI == null)
        {
            roleUI = new CmsRoleUI
            {
                RoleId = roleId,
                CreatedOn = DateTime.UtcNow,
                Properties = [],
                Tabs = [new() { Id = Constants.Ids.ContentTypeSystemTabId, IsSystemTab = true, SortOrder = 100, Name = "System" }],
                ExtendedData = new Dictionary<string, object>()
            };
            await db.StoreAsync(roleUI, cancellationToken);
        }

        return roleUI;
    }

    /// <summary>
    /// Saves role UI data
    /// </summary>
    public async Task<CmsRoleUI> SaveRoleUIAsync(CmsRoleUI roleUI, CancellationToken cancellationToken = default)
    {
        roleUI.ModifiedOn = DateTime.UtcNow;
        await db.StoreAsync(roleUI, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return roleUI;
    }
}
