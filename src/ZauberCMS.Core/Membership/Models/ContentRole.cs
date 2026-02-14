using System.Text.Json.Serialization;

namespace ZauberCMS.Core.Membership.Models;

/// <summary>
/// Content-Role relationship entity optimized for RavenDB.
/// Uses string IDs for references instead of navigation properties.
/// </summary>
public class ContentRole
{
    /// <summary>
    /// Reference to the Content document ID
    /// </summary>
    public string ContentId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the content (not serialized, loaded via Include)
    /// </summary>
    [JsonIgnore]
    public Content.Models.Content? Content { get; set; }

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
