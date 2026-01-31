using Electra.Core;
using Electra.Core.Entities;
using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Models.Geo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Electra.Persistence;


public class ElectraDbContext : IdentityDbContext<ElectraUser, ElectraRole, string,
    IdentityUserClaim<string>,
    IdentityUserRole<string>,
    IdentityUserLogin<string>,
    IdentityRoleClaim<string>,
    IdentityUserToken<string>>
{
    public ElectraDbContext(DbContextOptions<ElectraDbContext> options) : base(options)
    {
    }

    protected ElectraDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<AiUsageLog> AiUsageLogs { get; set; }
    public DbSet<AddressModel> Addresses { get; set; }
    public DbSet<ApiAccountModel> ApiAccounts { get; set; }
    public DbSet<ApiClaimsModel> ApiClaims { get; set; }
    public DbSet<CityModel> Cities { get; set; }
    public DbSet<CountryModel> Countries { get; set; }
    public DbSet<ElectraUserProfile> UserProfiles { get; set; }   
    public DbSet<UserPasskeys> UserPasskeys { get; set; }
    
    // Authentication token management
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<JwtSigningKey> JwtSigningKeys { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        ConfigureIdentityTables(builder);
        ConfigureDecimalPrecision(builder);
        ModelApiAuth(builder);
        ModelUserProfile(builder);
        ConfigureAuthenticationTokens(builder);
    }

    private void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<ElectraUser>(entity =>
        {
            entity.ToTable("Users", schema: Schemas.Auth);
            
            // Auditing - use ValueGeneratedOnAdd for server-side defaults
            entity.Property(x => x.CreatedOn).ValueGeneratedOnAdd();
            entity.Property(x => x.ModifiedOn).ValueGeneratedOnAdd();
            entity.HasIndex(x => x.CreatedOn);
            entity.HasIndex(x => x.ModifiedOn);
            entity.HasIndex(x => x.CreatedBy);
            entity.HasIndex(x => x.ModifiedBy);
            
            // Profile relationship - ONLY CONFIGURE ONCE
            entity.HasOne(x => x.Profile)
                .WithOne()
                .HasForeignKey<ElectraUserProfile>(x => x.Userid)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasIndex(i => i.UserProfileId).IsUnique();
        });

        builder.Entity<ElectraRole>(entity =>
        {
            entity.ToTable("Roles", schema: Schemas.Auth);
        });
        
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", schema: Schemas.Auth);
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", schema: Schemas.Auth);
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", schema: Schemas.Auth);
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", schema: Schemas.Auth);
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", schema: Schemas.Auth);
    }

    private void ConfigureDecimalPrecision(ModelBuilder builder)
    {
        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
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
                .ValueGeneratedOnAdd();

            entity.Property(x => x.ModifiedOn)
                .ValueGeneratedOnAdd();

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
                .ValueGeneratedOnAdd();

            entity.Property(x => x.ModifiedOn)
                .ValueGeneratedOnAdd();

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

    private void ConfigureAuthenticationTokens(ModelBuilder builder)
    {
        // Refresh tokens for session management
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", schema: Schemas.Auth);
            entity.HasKey(rt => rt.Id);
            entity.HasIndex(rt => rt.UserId);
            entity.HasIndex(rt => rt.TokenHash).IsUnique();
            entity.HasIndex(rt => rt.ExpiresAt);
            entity.HasIndex(rt => rt.RevokedAt);
            entity.Property(rt => rt.CreatedOn).ValueGeneratedOnAdd();
        });

        // JWT signing keys for key rotation
        builder.Entity<JwtSigningKey>(entity =>
        {
            entity.ToTable("JwtSigningKeys", schema: Schemas.Auth);
            entity.HasKey(jsk => jsk.Id);
            entity.HasIndex(jsk => jsk.KeyId).IsUnique();
            // Unique constraint: only one key can be current
            entity.HasIndex(jsk => jsk.IsCurrentSigningKey).IsUnique();
            entity.Property(jsk => jsk.CreatedOn).ValueGeneratedOnAdd();
        });
    }

    public override int SaveChanges()
    {
        AssignSnowflakeIds();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AssignSnowflakeIds();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void AssignSnowflakeIds()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Added, Entity: IEntity{ Id: null } entity })
            {
                entity.Id = Snowflake.NewId().ToString();
            }
        }
    }
}
