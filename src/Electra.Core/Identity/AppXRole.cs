using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Microbians.Core.Identity
{
    [Table("Roles")]
    public class AppXRole : IdentityRole
    {
        public AppXRole() { }

        public AppXRole(string roleName) : base(roleName) { }
        
        public virtual ICollection<IdentityRoleClaim<string>> Claims { get; set; } = new List<IdentityRoleClaim<string>>();
    }
}