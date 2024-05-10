using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Electra.Core.Identity
{
    [Table("Roles")]
    public class ElectraRole : IdentityRole
    {
        public ElectraRole() { }

        public ElectraRole(string roleName) : base(roleName) { }
        
        public virtual ICollection<IdentityRoleClaim<string>> Claims { get; set; } = new List<IdentityRoleClaim<string>>();
    }
}