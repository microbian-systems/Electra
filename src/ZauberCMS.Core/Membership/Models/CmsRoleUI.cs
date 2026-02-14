using System.Text.Json.Serialization;
using Electra.Core.Entities;
using ZauberCMS.Core.Shared.Models;

namespace ZauberCMS.Core.Membership.Models;

/// <summary>
/// UI-focused CMS role data stored as a separate RavenDB document.
/// Use Include(x => x.RoleId) to load the associated CmsRole.
/// </summary>
public class CmsRoleUI : Entity
{
    /// <summary>
    /// Reference to the CmsRole document ID for RavenDB Include() support
    /// </summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the role (not serialized, loaded via Include)
    /// </summary>
    [JsonIgnore]
    public CmsRole? Role { get; set; }

    /// <summary>
    /// Description of the role
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Icon for the role in the UI
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Extended data dictionary for custom properties
    /// </summary>
    public Dictionary<string, object> ExtendedData { get; set; } = new();

    /// <summary>
    /// The properties available on this Role
    /// </summary>
    public List<PropertyType> Properties { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of tabs associated with this Role
    /// </summary>
    /// <remarks>
    /// Tabs are used to organize the properties into separate sections.
    /// Each tab represents a logical grouping of properties.
    /// </remarks>
    public List<Tab> Tabs { get; set; } = [new() {Id = Constants.Ids.ContentTypeSystemTabId, IsSystemTab = true, SortOrder = 100, Name = "System"}];
}
