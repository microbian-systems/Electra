using Aero.Core.Identity;
using Aero.Models.Entities;

namespace Aero.RavenDB.Indexes;

using Raven.Client.Documents.Indexes;
using System.Linq;

public class Users_ByRoleName : AbstractIndexCreationTask<AeroUser, Users_ByRoleName.Result>
{
    public class Result
    {
        public string[] RoleNames { get; set; } = [];
    }

    public Users_ByRoleName()
    {
        Map = users => 
            from u in users
            let roles = u.Roles.Select(r => LoadDocument<AeroRole>(r.Id))
            select new Result
            {
                RoleNames = roles.Select(role => role.Name).ToArray() 
            };
    }
}
