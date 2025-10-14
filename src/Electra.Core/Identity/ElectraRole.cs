using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Electra.Core.Identity;


[Table("Roles")]
public class ElectraRole : ElectraRole<long>
{
    public ElectraRole() => Id = Snowflake.NewId();
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

    public virtual ICollection<IdentityRoleClaim<string>> Claims { get; set; } = new List<IdentityRoleClaim<string>>();
}