using System.Text.Json.Serialization;
using Electra.Core.Entities;
using Microsoft.AspNetCore.Identity;
using ZauberCMS.Core.Content.Interfaces;
using ZauberCMS.Core.Shared.Interfaces;

namespace ZauberCMS.Core.Membership.Models;

public class CmsUser : IdentityUser<string>, IEntity, ITreeItem, IHasPropertyValues
{
    public List<UserRole> UserRoles { get; set; } = [];

    //public Media? ProfileImage { get; set; }
    //public Guid? ProfileImageId { get; set; }

    /// <summary>
    /// The content properties
    /// </summary>n
    public List<UserPropertyValue> PropertyData { get; set; } = [];


    private Dictionary<string, string>? _contentValues;

    public Dictionary<string, string> ContentValues()
    {
        return _contentValues ??= PropertyData.ToDictionary(x => x.Alias, x => x.Value);
    }
    
    public Dictionary<string, object> ExtendedData { get; set; } = new();
    
    /// <summary>
    /// If parent ids are set this could have children
    /// </summary>
    [JsonIgnore]
    public List<Audit.Models.Audit> Audits { get; set; } = [];
    
    public string? Name
    {
        get => this.UserName;
        set => this.UserName = value;
    }

    public DateTimeOffset CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? ModifiedOn { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}