using Electra.Models;
using Electra.Models.Entities;

namespace Electra.Common.Web;

public class ApiAuthContext : DbContext
{
    public DbSet<ApiAccountModel> ApiAccounts { get; set; }
    public DbSet<ApiClaimsModel> Claims { get; set; }
    private const string schemaName = "apiauth";
    
    public ApiAuthContext(DbContextOptions<ApiAuthContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApiAccountModel>()
            .ToTable("ApiAccounts", schema: schemaName);
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
            .ToTable("ApiClaims", schema: schemaName)
            .HasKey(pk => pk.Id);
        builder.Entity<ApiClaimsModel>()
            .HasOne<ApiAccountModel>()
            .WithMany(m => m.Claims)
            .HasForeignKey(m => m.AccountId);
    }
}