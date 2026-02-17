using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Aero.Core.Identity;

// public class UserRole : IdentityUserRole<string>
// {
//     /// <summary>
//     /// Reference to the Role document ID
//     /// </summary>
//     public string RoleId { get; set; } = string.Empty;
//
//     /// <summary>
//     /// Navigation property to the role (not serialized, loaded via Include)
//     /// </summary>
//     [JsonIgnore]
//     public CmsRole? Role { get; set; }
//
//     /// <summary>
//     /// Reference to the User document ID
//     /// </summary>
//     public string UserId { get; set; } = string.Empty;
//
//     /// <summary>
//     /// Navigation property to the user (not serialized, loaded via Include)
//     /// </summary>
//     [JsonIgnore]
//     public AeroUser? User { get; set; }
// }

[Table("Roles")]
public class AeroRole : AeroRole<string>
{
    public AeroRole() => Id = Snowflake.NewId().ToString();
    public AeroRole(string roleName)
        : this()
    {
        this.Name = roleName;
    }
}


[Table("Roles")]
public class AeroRole<Tkey> : IdentityRole<Tkey>
    where Tkey : IEquatable<Tkey>, IComparable<Tkey>
{
    public AeroRole() { }

    public AeroRole(string roleName) : base(roleName) { }

    public List<IdentityRoleClaim<string>> Claims { get; set; } = [];
    public List<string> Users { get; set; } = [];
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedOn { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}