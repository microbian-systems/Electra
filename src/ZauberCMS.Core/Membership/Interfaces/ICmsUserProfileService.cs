using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Membership.Interfaces;

/// <summary>
/// Service for managing CMS user profiles and role UI data.
/// Provides RavenDB-optimized queries using Include() patterns.
/// </summary>
public interface ICmsUserProfileService
{
    /// <summary>
    /// Gets a user profile by user ID
    /// </summary>
    Task<CmsUserProfile?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user profile with the associated user loaded via Include()
    /// </summary>
    Task<(CmsUser? User, CmsUserProfile? Profile)> GetUserWithProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a user profile for the specified user
    /// </summary>
    Task<CmsUserProfile> GetOrCreateProfileAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a user profile
    /// </summary>
    Task<CmsUserProfile> SaveProfileAsync(CmsUserProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role UI data by role ID
    /// </summary>
    Task<CmsRoleUI?> GetRoleUIAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role UI data with the associated role loaded via Include()
    /// </summary>
    Task<(CmsRole? Role, CmsRoleUI? UI)> GetRoleWithUIAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates role UI data for the specified role
    /// </summary>
    Task<CmsRoleUI> GetOrCreateRoleUIAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves role UI data
    /// </summary>
    Task<CmsRoleUI> SaveRoleUIAsync(CmsRoleUI roleUI, CancellationToken cancellationToken = default);
}
