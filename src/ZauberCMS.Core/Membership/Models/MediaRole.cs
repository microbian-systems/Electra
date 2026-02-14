using System.Text.Json.Serialization;

namespace ZauberCMS.Core.Membership.Models;

/// <summary>
/// Media-Role relationship entity optimized for RavenDB.
/// Uses string IDs for references instead of navigation properties.
/// </summary>
public class MediaRole
{
    /// <summary>
    /// Reference to the Media document ID
    /// </summary>
    public string MediaId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the media (not serialized, loaded via Include)
    /// </summary>
    [JsonIgnore]
    public Media.Models.Media? Media { get; set; }

    /// <summary>
    /// Reference to the Role document ID
    /// </summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the role (not serialized, loaded via Include)
    /// </summary>
    [JsonIgnore]
    public CmsRole? Role { get; set; }
}
