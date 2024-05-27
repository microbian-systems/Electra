namespace Electra.Persistence;

public class ElectraDbContext(DbContextOptions<ElectraDbContext<ElectraUser, ElectraRole>> options)
    : ElectraDbContext<ElectraUser>(options);

public class ElectraDbContext<T>(DbContextOptions<ElectraDbContext<T, ElectraRole>> options)
    : ElectraDbContext<T, ElectraRole>(options)
    where T : ElectraUser;

public class ElectraDbContext<T, TRole>(DbContextOptions<ElectraDbContext<T, TRole>> options)
    : IdentityDbContext<T, TRole, Guid>(options)
    where T : ElectraUser
    where TRole : IdentityRole<Guid>
{
    protected const string schema = "Users";

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

        builder.Entity<TRole>(entity => { entity.ToTable(name: "Roles"); });
        builder.Entity<IdentityUserRole<string>>(entity => { entity.ToTable("UserRoles"); });

        builder.Entity<IdentityUserClaim<string>>(entity => { entity.ToTable("UserClaims"); });

        builder.Entity<IdentityUserLogin<string>>(entity => { entity.ToTable("UserLogins"); });

        builder.Entity<IdentityUserClaim<string>>(entity => { entity.ToTable("RoleClaims"); });

        builder.Entity<IdentityUserToken<string>>(entity => { entity.ToTable("UserTokens"); });

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
        //     .OnDelete(DeleteBehavior.Cascade);
    }
}