using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Electra.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Electra.Core.Identity;

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
//     public ElectraUser? User { get; set; }
// }

[Table("Roles")]
public class ElectraRole : ElectraRole<string>
{
    public ElectraRole() => Id = Snowflake.NewId().ToString();
    public ElectraRole(string roleName)
        : this()
    {
        this.Name = roleName;
    }
}


[Table("Roles")]
public class ElectraRole<Tkey> : IdentityRole<Tkey>
    where Tkey : IEquatable<Tkey>, IComparable<Tkey>
{
    public ElectraRole() { }

    public ElectraRole(string roleName) : base(roleName) { }

    public List<IdentityRoleClaim<string>> Claims { get; set; } = [];
    public List<string> Users { get; set; } = [];
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedOn { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}