using System.Text.Json.Serialization;
using Electra.Core.Identity;

namespace ZauberCMS.Core.Membership.Models;

/// <summary>
/// CMS Role entity focused on Identity concerns only.
/// UI-specific data (Description, Icon, Properties, Tabs, etc.) has been moved to CmsRoleUI.
/// Inherits from ElectraRole which contains the Users list for role assignments.
/// </summary>
public class CmsRole : ElectraRole
{
    /// <summary>
    /// Content access permissions for this role
    /// </summary>
    [JsonIgnore]
    public List<ContentRole> ContentRoles { get; set; } = [];

    /// <summary>
    /// Media access permissions for this role
    /// </summary>
    [JsonIgnore]
    public List<MediaRole> MediaRoles { get; set; } = [];
}
