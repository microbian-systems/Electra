using System.Text.Json.Serialization;
using Electra.Core.Entities;
using Electra.Models.Entities;
using ZauberCMS.Core.Content.Interfaces;
using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Membership.Models;

/// <summary>
/// CMS-specific user profile data stored as a separate RavenDB document.
/// Use Include(x => x.UserId) to load the associated ElectraUser.
/// </summary>
public class CmsUserProfile : Entity, IHasPropertyValues
{
    /// <summary>
    /// Reference to the ElectraUser document ID for RavenDB Include() support
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the user (not serialized, loaded via Include)
    /// </summary>
    [JsonIgnore]
    public ElectraUser? User { get; set; }

    /// <summary>
    /// Property values for this user (embedded in the document)
    /// </summary>
    public List<UserPropertyValue> PropertyData { get; set; } = [];

    /// <summary>
    /// Extended data dictionary for custom properties
    /// </summary>
    public Dictionary<string, object> ExtendedData { get; set; } = new();

    /// <summary>
    /// Reference to the profile image media item
    /// </summary>
    public string? ProfileImageId { get; set; }

    private Dictionary<string, string>? _contentValues;

    /// <summary>
    /// Gets property values as a dictionary keyed by alias
    /// </summary>
    public Dictionary<string, string> ContentValues()
        => _contentValues ??= PropertyData.ToDictionary(x => x.Alias, x => x.Value);

    /// <summary>
    /// Resets the cached content values dictionary
    /// </summary>
    public void ResetContentValues() => _contentValues = null;
}
