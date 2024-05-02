using Piranha.Data;
using Piranha.Data.EF;
using Db = Piranha.Data.EF;
using Piranha.AspNetCore.Identity;

namespace Electra.Piranha.Persistence;

/*public abstract class ElectraDb<T>(DbContextOptions<T> options)
    : Piranha.AspNetCore.Identity.Db<T>(options)
    where T : DbContext, Piranha.AspNetCore.Identity.Db<T>
{
    /// <summary>
    ///     Creates and configures the data model.
    /// </summary>
    /// <param name="mb">The current model builder</param>
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<User>().ToTable("Electra.Users");
        mb.Entity<Role>().ToTable("Piranha.Roles");
        mb.Entity<IdentityUserClaim<Guid>>().ToTable("Piranha.UserClaims");
        mb.Entity<IdentityUserRole<Guid>>().ToTable("Piranha.UserRoles");
        mb.Entity<IdentityUserLogin<Guid>>().ToTable("Piranha.UserLogins");
        mb.Entity<IdentityRoleClaim<Guid>>().ToTable("Piranha.RoleClaims");
        mb.Entity<IdentityUserToken<Guid>>().ToTable("Piranha.UserTokens");
    }
}*/