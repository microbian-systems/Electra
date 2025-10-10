using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Electra.Web.BlogEngine.Data;

/// <summary>
/// Database context for blog functionality
/// </summary>
public class BlogDbContext : DbContext
{
    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Blog posts collection
    /// </summary>
    public DbSet<Entities.BlogEntry> Blogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Blog entity
        modelBuilder.Entity<Entities.BlogEntry>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.Id);

            // Indexes for performance
            entity.HasIndex(e => e.IsPublished);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.IsFeatured);

            // Configure array properties as JSON columns
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? new string[]{})
                .HasColumnType("TEXT");

            entity.Property(e => e.Authors)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null) ?? new string[]{})
                .HasColumnType("TEXT");

            // Configure string length constraints
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500);

            entity.Property(e => e.Slug)
                .HasMaxLength(250)
                .IsRequired();

            // Configure enum as string
            entity.Property(e => e.ContentType)
                .HasConversion<string>();

            // Default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps
        var entries = ChangeTracker.Entries<Entities.BlogEntry>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            entry.Entity.UpdatedAt = DateTime.UtcNow;

            // Generate slug if not provided
            if (string.IsNullOrEmpty(entry.Entity.Slug))
            {
                entry.Entity.Slug = GenerateSlug(entry.Entity.Title);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrEmpty(title))
            return Guid.NewGuid().ToString()[..8];

        // Basic slug generation
        var slug = title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("'", "")
            .Replace("\"", "");

        // Remove multiple dashes
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        // Trim dashes from start and end
        slug = slug.Trim('-');

        // Limit length
        if (slug.Length > 200)
        {
            slug = slug[..200].TrimEnd('-');
        }

        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString()[..8] : slug;
    }
}