using Electra.Auth.Models;
using Electra.Core;
using Electra.Models;

namespace Electra.Auth.Context;

public sealed class ElectraIdentityRole : IdentityRole<long>
{
    public ElectraIdentityRole() => Id = Snowflake.NewId();

    /// <summary>
    /// Initializes a new instance of <see cref="T:Electra.Auth.ElectraIdentityRole" />.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    /// <remarks>
    /// The Id property is initialized to form a new GUID string value.
    /// </remarks>
    public ElectraIdentityRole(string roleName)
        : this()
    {
        this.Name = roleName;
    }
}

public sealed class ElectraAuthDbContext(DbContextOptions<ElectraAuthDbContext> options)
    : IdentityDbContext<
        ElectraUser, 
        ElectraIdentityRole, 
        long, 
        IdentityUserClaim<long>, 
        IdentityUserRole<long>, 
        IdentityUserLogin<long>, 
        IdentityRoleClaim<long>, 
        IdentityUserToken<long>>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaNames.Auth);

        // Customize the ASP.NET Identity model and override table names
        builder.Entity<ElectraUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
    }
}