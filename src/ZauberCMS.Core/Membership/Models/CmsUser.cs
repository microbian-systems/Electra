using Electra.Models.Entities;

namespace ZauberCMS.Core.Membership.Models;

/// <summary>
/// CMS User entity focused on Identity concerns only.
/// CMS-specific data (PropertyData, ExtendedData, etc.) has been moved to CmsUserProfile.
/// </summary>
public class CmsUser : ElectraUser
{
    /// <summary>
    /// User role assignments
    /// </summary>
    public List<UserRole> UserRoles { get; set; } = [];
}
