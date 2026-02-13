using System.Collections.Generic;
using Electra.Core.Identity;
using Electra.Models.Entities;

namespace Electra.Persistence.RavenDB.Indexes;

using Raven.Client.Documents.Indexes;
using System.Linq;

public class Users_ByRoleName : AbstractIndexCreationTask<ElectraUser, Users_ByRoleName.Result>
{
    public class Result
    {
        public string[] RoleNames { get; set; } = [];
    }

    public Users_ByRoleName()
    {
        Map = users => 
            from u in users
            let roles = u.Roles.Select(r => LoadDocument<ElectraRole>(r.Id))
            select new Result
            {
                RoleNames = roles.Select(role => role.Name).ToArray() 
            };
    }
}
