using Electra.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents.Indexes;

namespace Electra.Persistence.RavenDB;

/// <summary>
/// Index to user when querying users.
/// </summary>
public class IdentityUserIndex<TUser> : AbstractIndexCreationTask<TUser, IdentityUserIndex<TUser>.Result> where TUser : ElectraUser
{
    /// <summary>
    /// Result from a query to the IdentityUserIndex.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// The user name.
        /// </summary>
        public string UserName { get; set; } = string.Empty;
        /// <summary>
        /// The email.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// The login provider identifiers.
        /// </summary>
        public List<string>? LoginProviderIdentifiers { get; set; } = [];

        /// <summary>
        /// The roles assigned to the user.
        /// </summary>
        public List<string>? Roles { get; set; } = [];
    }

    /// <summary>
    /// Creates the map.
    /// </summary>
    public IdentityUserIndex()
    {
        Map = users => from user in users
            select new Result
            {
                UserName = user.UserName,
                Email = user.Email,
                LoginProviderIdentifiers = user.Logins.Select(x => x.LoginProvider + "|" + x.ProviderKey).ToList(),
                Roles = user.Roles.Select(x => x.RoleId).ToList()
            };
    }

    /// <inheritdoc />
    public override string IndexName => "IdentityUserIndex";
}