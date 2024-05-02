using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Extensions;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Options;
using Electra.Core.Identity;
using Electra.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Electra.Persistence;

[Table("Roles")]
public class RoleConfiguration : IEntityTypeConfiguration<ElectraRole>
{
    public void Configure(EntityTypeBuilder<ElectraRole> builder)
    {
        builder.HasMany(r => r.Claims)
            .WithOne()
            .HasForeignKey(r => r.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasData(
            new ElectraRole
            {
                Name = "Admin",
                NormalizedName = "ADMIN"
            },
            new ElectraRole
            {
                Name = "PiranhaAdmin",
                NormalizedName = "PIRANHAADMIN"
            },
            new ElectraRole
            {
                Name = "Basic",
                NormalizedName = "BASIC"
            },
            new ElectraRole
            {
                Name = "Provider",
                NormalizedName = "PROVIDER"
            },
            new ElectraRole
            {
                Name = "Standard",
                NormalizedName = "STANDARD"
            }
        );
    }
}

// todo - consider inheriting from Piranha.Data.IDb to enable identity features w/ piranha
public class ElectraIdentityContext : IdentityDbContext<ElectraUser, ElectraRole, string>, IPersistedGrantDbContext
{
    private const string schema = "Users";  // todo - change default schema to app from "Users"
    private readonly OperationalStoreOptions operationalStoreOptions;
    
    //public DbSet<T> Users { get; set; }
    // todo - determine what format to store the profile
    // todo - later denormalize if join performance costs too much (cache first, then denormalize)
    // todo - add foreign key to the Users (AspNetUsers) table
    // https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cpoco
    //public DbSet<bldProfile> UserProfiles { get; set; }
    //public DbSet<CustomerProfile> CustomerProfile { get; set; }
    //public DbSet<ProviderProfile> ProviderProfile { get; set; }
    public DbSet<PersistedGrant> PersistedGrants { get; set; }
    public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }
    public DbSet<Duende.IdentityServer.EntityFramework.Entities.Key> Keys { get; set; }
    public DbSet<ServerSideSession> ServerSideSessions { get; set; }
    public DbSet<PushedAuthorizationRequest> PushedAuthorizationRequests { get; set; }

    public ElectraIdentityContext(DbContextOptions<ElectraIdentityContext> options) 
        : base(options)
    //IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
    {
        //operationalStoreOptions.Value.DefaultSchema = schema;
        operationalStoreOptions = new OperationalStoreOptions {DefaultSchema = schema};
    }

    Task<int> IPersistedGrantDbContext.SaveChangesAsync() => base.SaveChangesAsync();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(schema);
        IgnoreTables(builder);


        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }

        builder.Entity<ElectraUser>(entity =>
        {
            entity.ToTable(name: "Users");
            // todo - was specific to ElectraUser
            //entity.HasBaseType<ElectraUser>();
            entity.Property(x => x.CreatedOn)
                .IsRequired()
                .HasDefaultValue(DateTimeOffset.UtcNow);

            entity.Property(x => x.ModifiedOn)
                .HasDefaultValue(DateTimeOffset.UtcNow);
            
            entity.HasIndex(x => x.CreatedOn);
            entity.HasIndex(x => x.ModifiedOn);
            entity.HasIndex(x => x.CreatedBy);
            entity.HasIndex(x => x.ModifiedBy);
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.IsDeleted);
            entity.HasIndex(x => x.Birthday);
            entity.HasIndex(x => x.PhoneNumber);
            entity.HasIndex(x => x.NormalizedEmail);
            entity.HasIndex(x => x.NormalizedUserName);
        });

        builder.Entity<ElectraUser>()
            .HasMany(p => p.Roles)
            .WithOne()
            .HasForeignKey(p => p.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<ElectraUser>()
            .HasMany(e => e.Claims)
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.Entity<ElectraRole>()
            .HasBaseType<IdentityRole>()
            .ToTable("Roles")
            .HasMany(r => r.Claims)
            .WithOne()
            .HasForeignKey(r => r.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<ElectraUser>()
            .HasMany(r => r.Logins)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<ElectraUser>()
            .HasMany(r => r.Tokens)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // builder.Entity<ElectraUser>()
        //     .HasOne<bldProfile>(x => x.Profile)
        //     .WithOne()
        //     .HasForeignKey<bldProfile>(x => x.UserId);
        // builder.Entity<bldProfile>()
        //     .ToTable("Profiles")
        //     .HasOne<ElectraUser>()
        //     .WithOne()
        //     .HasForeignKey<bldProfile>(x => x.UserId)
        //     .OnDelete(DeleteBehavior.Cascade)
        //     ;
        

        AddIdentityModelConfigs(builder);
        builder.ApplyConfiguration(new RoleConfiguration());
        builder.ConfigurePersistedGrantContext(operationalStoreOptions);
    }

    private void IgnoreTables(ModelBuilder builder)
    {
        builder.Ignore<Electra.Models.AddressModel>();
    }


protected virtual void AddIdentityModelConfigs(ModelBuilder builder)
    {
        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });
        
        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
            entity.HasKey(x => new { x.LoginProvider, x.ProviderKey });
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });
    }
}
// todo - fix Electra IdentityServerRegistrations in Electra persistence library
[Obsolete("marking obsolete until randon EF error is fixed", true)]
public class ElectraIdentityServerContext : ElectraIdentityServerContext<ElectraUser>, IPersistedGrantDbContext
{
    public ElectraIdentityServerContext(DbContextOptions options) 
        : base(options)
    {
    }
}

[Obsolete("marking obsolete until randon EF error is fixed", true)]
public class ElectraIdentityServerContext<T> : ElectraIdentityServerContext<T, ElectraRole>, IPersistedGrantDbContext
    where T : ElectraUser
{
    public ElectraIdentityServerContext(DbContextOptions options) 
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(schema);
        base.OnModelCreating(builder);
        builder.ConfigurePersistedGrantContext(operationalStoreOptions);
        
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
        
        // builder.Entity<ElectraUserProfile>()
        //     .HasOne<ElectraUser>(x => x.User)
        //     .WithOne()
        //     .HasForeignKey<ElectraUserProfile>(x => x.UserId)
        //     .OnDelete(DeleteBehavior.Cascade)
        //     ;
    }
}
    
[Obsolete("marking obsolete until randon EF error is fixed", true)]
public class ElectraIdentityServerContext<T, TRole> : ElectraIdentityServerContext<T, TRole, string>, IPersistedGrantDbContext
    where TRole : IdentityRole<string> 
    where T : IdentityUser<string>
{
    public ElectraIdentityServerContext(DbContextOptions options) : base(options)
    {
    }
}
    
[Obsolete("marking obsolete until randon EF error is fixed", true)]
public abstract class ElectraIdentityServerContext<T, TRole, TKey> : IdentityDbContext<T, TRole, TKey>, IPersistedGrantDbContext 
    where T : IdentityUser<TKey> 
    where TRole : IdentityRole<TKey> 
    where TKey : IEquatable<TKey>
{
    protected const string schema = "Users";
    protected readonly OperationalStoreOptions operationalStoreOptions;
    
    //public DbSet<T> Users { get; set; }
    // todo - determine what format to store the profile
    // todo - later denormalize if join performance costs too much (cache first, then denormalize)
    // todo - add foreign key to the Users (AspNetUsers) table
    // https://www.npgsql.org/efcore/mapping/json.html?tabs=data-annotations%2Cpoco
    public DbSet<ElectraUserProfile> UserProfileModels { get; set; }
    public DbSet<PersistedGrant> PersistedGrants { get; set; }
    public DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }
    public DbSet<Key> Keys { get; set; }
    public DbSet<ServerSideSession> ServerSideSessions { get; set; }
    public DbSet<PushedAuthorizationRequest> PushedAuthorizationRequests { get; set; }

    protected ElectraIdentityServerContext(DbContextOptions options) : base(options)
    {
        //operationalStoreOptions.Value.DefaultSchema = schema;
        operationalStoreOptions = new OperationalStoreOptions {DefaultSchema = schema};
    }

    Task<int> IPersistedGrantDbContext.SaveChangesAsync() => base.SaveChangesAsync();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(schema);
        builder.ConfigurePersistedGrantContext(operationalStoreOptions);
        
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
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable(name: "Roles");
        });

        AddIdentityModelConfigs(builder);
    }

    protected virtual void AddIdentityModelConfigs(ModelBuilder builder)
    {
        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });
    }
}