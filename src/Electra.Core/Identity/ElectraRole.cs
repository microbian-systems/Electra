using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Electra.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Electra.Core.Identity;


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
public class ElectraRole<Tkey> : IdentityRole<Tkey>, IEntity
    where Tkey : IEquatable<Tkey>, IComparable<Tkey>
{
    public ElectraRole() { }

    public ElectraRole(string roleName) : base(roleName) { }

    public virtual ICollection<IdentityRoleClaim<string>> Claims { get; set; } = new List<IdentityRoleClaim<string>>();

    public virtual List<string> Users { get; set; } = [];
    public new string Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ModifiedOn { get; set; } = DateTimeOffset.UtcNow;
    public string CreatedBy { get; set; }
    public string ModifiedBy { get; set; }
}