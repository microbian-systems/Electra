using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Models.Geo;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Electra.Persistence;

public class ElectraDbContextOptions : DbContextOptions<ElectraIdentityDbContext<ElectraUser, ElectraRole>>;

public class ElectraDbContext(ElectraDbContextOptions options) : ElectraIdentityDbContext<ElectraUser, ElectraRole>(options)
{
    public DbSet<AiUsageLog> AiUsageLogs { get; set; }
    public DbSet<AddressModel> Addresses { get; set; }
    public DbSet<ApiAccountModel> ApiAccounts { get; set; }
    public DbSet<ApiClaimsModel> ApiClaims { get; set; }
    public DbSet<CityModel> Cities { get; set; }
    public DbSet<CountryModel> Countries { get; set; }
    public DbSet<ElectraUserProfile> UserProfiles { get; set; }    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ModelApiAuth(builder);
        ModelUserProfile(builder);
        base.OnModelCreating(builder);
    }

    protected virtual void ModelUserProfile(ModelBuilder builder)
    {
        const string schemaName = "Users";
        base.OnModelCreating(builder);

        builder.Entity<ElectraUserProfile>()
            .ToTable("UserProfiles", schema: schemaName);
        builder.Entity<ElectraUserProfile>()
            .HasKey(i => i.Id);
        builder.Entity<ElectraUserProfile>()
            .HasIndex(i => i.Email);
    }

    protected virtual void ModelApiAuth(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApiAccountModel>()
            .ToTable("ApiAccounts", schema: "Electra");
        builder.Entity<ApiAccountModel>()
            .HasKey(i => i.Id);
        builder.Entity<ApiAccountModel>()
            .HasIndex(i => i.ApiKey, "ix_apikey")
            .IsUnique();
        builder.Entity<ApiAccountModel>()
            .HasIndex(i => i.Email);
        builder.Entity<ApiAccountModel>()
            .HasIndex(i => i.Enabled);
        builder.Entity<ApiAccountModel>()
            .HasIndex(i => i.CreateDate);
        builder.Entity<ApiAccountModel>()
            .HasIndex(i => i.ModifiedDate);

        builder.Entity<ApiClaimsModel>()
            .HasIndex(i => i.ClaimKey);
        builder.Entity<ApiClaimsModel>()
            .HasIndex(i => i.ClaimValue);

        builder.Entity<ApiAccountModel>()
            .HasMany<ApiClaimsModel>()
            .WithOne();

        builder.Entity<ApiClaimsModel>()
            .ToTable("ApiClaims", schema: "Electra")
            .HasKey(pk => pk.Id);
        builder.Entity<ApiClaimsModel>()
            .HasOne<ApiAccountModel>()
            .WithMany(m => m.Claims)
            .HasForeignKey(m => m.AccountId);
    }   
}

public class ElectraIdentityDbContext(DbContextOptions<ElectraIdentityDbContext<ElectraUser, ElectraRole>> options)
    : ElectraIdentityDbContext<ElectraUser>(options);

public class ElectraIdentityDbContext<T>(DbContextOptions<ElectraIdentityDbContext<T, ElectraRole>> options)
    : ElectraIdentityDbContext<T, ElectraRole>(options)
    where T : ElectraUser;

public class ElectraIdentityDbContext<T, TRole>(DbContextOptions<ElectraIdentityDbContext<T, TRole>> options)
    : IdentityDbContext<T, TRole, long>(options)
    where T : ElectraUser
    where TRole : IdentityRole<long>
{
    protected const string schema = "Electra";

    public DbSet<ElectraUserProfile> UserProfile { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(schema);
        base.OnModelCreating(builder); // todo - this may or may not work? verify....

        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        base.OnModelCreating(builder);

        builder.Entity<T>(entity =>
        {
            entity.ToTable(name: "Users");
            entity.Property(x => x.CreatedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.Property(x => x.ModifiedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.HasIndex(x => x.CreatedOn);
            entity.HasIndex(x => x.ModifiedOn);
            entity.HasIndex(x => x.CreatedBy);
            entity.HasIndex(x => x.ModifiedBy);
        });

        builder.Entity<TRole>(entity => { entity.ToTable(name: "Roles"); });
        builder.Entity<IdentityUserRole<long>>(entity => { entity.ToTable("UserRoles"); });

        builder.Entity<IdentityUserClaim<long>>(entity => { entity.ToTable("UserClaims"); });

        builder.Entity<IdentityUserLogin<long>>(entity => { entity.ToTable("UserLogins"); });

        builder.Entity<IdentityRoleClaim<long>>(entity => { entity.ToTable("RoleClaims"); });

        builder.Entity<IdentityUserToken<long>>(entity => { entity.ToTable("UserTokens"); });

        builder.Entity<T>()
            .HasMany(p => p.Roles)
            .WithOne()
            .HasForeignKey(p => p.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<T>()
            .HasMany(e => e.Claims)
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ElectraRole>()
            .HasMany(r => r.Claims)
            .WithOne()
            .HasForeignKey(r => r.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<T>()
            .HasMany(r => r.Logins)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<T>()
            .HasMany(r => r.Tokens)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<T>()
            .HasOne<ElectraUserProfile>(x => x.Profile)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}