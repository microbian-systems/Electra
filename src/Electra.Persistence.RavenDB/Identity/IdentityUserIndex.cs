using System.Collections.Generic;
using System.Linq;
using Electra.Models.Entities;
using Raven.Client.Documents.Indexes;

namespace Electra.Persistence.RavenDB.Identity;

/// <summary>
/// Index to user when querying users.
/// </summary>
public class IdentityUserIndex : AbstractIndexCreationTask<ElectraUser, IdentityUserIndex.Result>
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

        /// <summary>
        /// The WebAuthn credential identifiers.
        /// </summary>
        public List<byte[]>? WebAuthnCredentialIds { get; set; } = [];
    }

    /// <summary>
    /// Creates the map.
    /// </summary>
    public IdentityUserIndex()
    {
        Map = (IEnumerable<ElectraUser> users) => from u in users
            select new Result
            {
                UserName = u.UserName!,
                Email = u.Email!,
                LoginProviderIdentifiers = u.Logins.Select(x => x.LoginProvider + "|" + x.ProviderKey).ToList(),
                Roles = u.Roles.Select(x => x.Name).ToList(),
            };
    }

    /// <inheritdoc />
    public override string IndexName => $"Identity/Users/ByDetails";
}