namespace Electra.Persistence;

public class ElectraDbContext(DbContextOptions<ElectraDbContext> options)
    : ElectraDbContext<ElectraUser>(options);

public class ElectraDbContext<T>(DbContextOptions options)
    : ElectraDbContext<T, ElectraRole>(options)
    where T : ElectraUser, IEquatable<T>;

public class ElectraDbContext<T, TRole>(DbContextOptions options)
    : IdentityDbContext<T, TRole, Guid>(options)
    where T : ElectraUser, IEquatable<T>
    where TRole : IdentityRole<Guid>
{
    protected const string schema = "Users";

    public new DbSet<T> Users { get; set; }
    public DbSet<ElectraUserProfile> UserProfile { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(schema);

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

        builder.Entity<TRole>(entity =>
        {
            entity.ToTable(name: "Roles");
            entity.HasKey(x => x.Id);
        });
        builder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(x => new { x.UserId, x.RoleId });
        });

        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("UserClaims");
            entity.HasKey(x => x.Id);
        });

        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("UserLogins");
            entity.HasKey(x => x.UserId);
        });

        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("RoleClaims");
            entity.HasKey(x => x.Id);
        });

        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("UserTokens");
            entity.HasKey(x => new { x.UserId, x.LoginProvider, x.Name });
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
            .HasForeignKey<ElectraUserProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}