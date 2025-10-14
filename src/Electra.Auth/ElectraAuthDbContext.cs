using System.Linq;
using Electra.Core;
using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Persistence;

namespace Electra.Auth;

public sealed class ElectraIdentityRole : IdentityRole<long>
{
    public ElectraIdentityRole() => Id = Snowflake.NewId();

    /// <summary>
    /// Initializes a new instance of <see cref="T:ElectraIdentityRole" />.
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

public class ElectraAuthDbContext(DbContextOptions<ElectraAuthDbContext> options)
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
    public DbSet<AiUsageLog> AiUsageLogs { get; set; }
    public DbSet<AddressModel> Addresses { get; set; }
    public DbSet<ApiAccountModel> ApiAccounts { get; set; }
    public DbSet<ApiClaimsModel> ApiClaims { get; set; }
    public DbSet<CityModel> Cities { get; set; }
    public DbSet<CountryModel> Countries { get; set; }
    public DbSet<ElectraUserProfile> UserProfiles { get; set; }
    public DbSet<UserPasskeys> UserPasskeys { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ModelApiAuth(builder);
        ModelUserProfile(builder);
        builder.HasDefaultSchema(Schemas.Auth);

        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        builder.Entity<ElectraRole>(entity =>
        {
            entity
                .HasMany(r => r.Claims)
                .WithOne()
                .HasForeignKey(r => r.RoleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Customize the ASP.NET Identity model and override table names
        builder.Entity<ElectraUser>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(x => x.CreatedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.Property(x => x.ModifiedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.HasIndex(x => x.CreatedOn);
            entity.HasIndex(x => x.ModifiedOn);
            entity.HasIndex(x => x.CreatedBy);
            entity.HasIndex(x => x.ModifiedBy);
            
            entity.HasOne<ElectraUserProfile>()
                .WithOne()
                .HasForeignKey<ElectraUserProfile>(x => x.Userid)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(i => i.UserProfileId)
                .IsUnique();
            
            entity
                .HasMany(p => p.Roles)
                .WithOne()
                .HasForeignKey(p => p.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(e => e.Claims)
                .WithOne()
                .HasForeignKey(e => e.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(r => r.Logins)
                .WithOne()
                .HasForeignKey(r => r.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(r => r.Tokens)
                .WithOne()
                .HasForeignKey(r => r.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne<ElectraUserProfile>(x => x.Profile)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<IdentityRole<long>>().ToTable("Roles", schema: Schemas.Auth);
        builder.Entity<IdentityUserRole<long>>().ToTable("UserRoles", schema: Schemas.Auth);
        builder.Entity<IdentityUserClaim<long>>().ToTable("UserClaims", schema: Schemas.Auth);
        builder.Entity<IdentityUserLogin<long>>().ToTable("UserLogins", schema: Schemas.Auth);
        builder.Entity<IdentityRoleClaim<long>>().ToTable("RoleClaims", schema: Schemas.Auth);
        builder.Entity<IdentityUserToken<long>>().ToTable("UserTokens", schema: Schemas.Auth);
    }

    protected virtual void ModelUserProfile(ModelBuilder builder)
    {
        builder.Entity<ElectraUserProfile>(entity =>
        {
            entity.ToTable("UserProfiles", schema: Schemas.Users);
            entity.HasOne<ElectraUser>()
                .WithOne(x => x.Profile)
                .HasForeignKey<ElectraUserProfile>(x => x.Userid)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.Userid).IsUnique();
            entity.Property(x => x.CreatedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.Property(x => x.ModifiedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.CreatedOn);
            entity.HasIndex(x => x.ModifiedOn);
            entity.HasIndex(x => x.CreatedBy);
            entity.HasIndex(x => x.ModifiedBy);
        });
    }

    protected void ModelApiAuth(ModelBuilder builder)
    {
        builder.Entity<ApiAccountModel>(entity =>
        {
            entity.ToTable("ApiAccounts", schema: Schemas.Electra);
            entity.HasIndex(i => i.ApiKey, "ix_apikey")
                .IsUnique();
            entity.Property(x => x.CreatedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.Property(x => x.ModifiedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.HasIndex(x => x.CreatedOn);
            entity.HasIndex(x => x.ModifiedOn);
            entity.HasIndex(x => x.CreatedBy);
            entity.HasIndex(x => x.ModifiedBy);

            entity.HasMany<ApiClaimsModel>()
                .WithOne();
        });

        builder.Entity<ApiClaimsModel>(entity =>
        { // todo - verify ApiClaimsModel requries an int pkey - we should keep consistent and inherit from EntityBase<long>
            entity.ToTable("ApiClaims", schema: Schemas.Electra);
            entity.HasIndex(i => i.ClaimKey);
            entity.HasIndex(i => i.ClaimValue);
            entity.HasOne<ApiAccountModel>()
                .WithMany(m => m.Claims)
                .HasForeignKey(m => m.AccountId);
            
        });
    }
}