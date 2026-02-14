using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace ZauberCMS.Core.Membership.Models;

/// <summary>
/// User-Role relationship entity optimized for RavenDB.
/// Uses string IDs for references instead of navigation properties.
/// </summary>
public class UserRole : IdentityUserRole<string>
{
    /// <summary>
    /// Reference to the Role document ID
    /// </summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the role (not serialized, loaded via Include)
    /// </summary>
    [JsonIgnore]
    public CmsRole? Role { get; set; }

    /// <summary>
    /// Reference to the User document ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the user (not serialized, loaded via Include)
    /// </summary>
    [JsonIgnore]
    public CmsUser? User { get; set; }
}
