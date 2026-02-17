# Aero.EfCore

Entity Framework Core integration for the Aero framework with PostgreSQL support.

## Overview

`Aero.EfCore` provides Entity Framework Core implementations of the Aero repository interfaces. It's optimized for PostgreSQL but can work with any EF Core-supported database.

## Key Components

### GenericEntityFrameworkRepository<T, TKey>

The main EF Core repository implementation:

```csharp
public class GenericEntityFrameworkRepository<T, TKey> : GenericRepository<T, TKey>, 
    IGenericEntityFrameworkRepository<T, TKey>
    where T : class, IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected readonly DbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericEntityFrameworkRepository(
        DbContext context, 
        ILogger<GenericEntityFrameworkRepository<T, TKey>> log) : base(log)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public override async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public override async Task<T> FindByIdAsync(TKey id)
    {
        return await DbSet.FindAsync(id);
    }

    public override async Task<T> InsertAsync(T entity)
    {
        entity.Id = entity.Id ?? GenerateKey();
        entity.CreatedOn = DateTimeOffset.UtcNow;
        
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    // ... additional implementations
}
```

### AeroDbContext

Base DbContext with common configurations:

```csharp
public class AeroDbContext : DbContext
{
    public AeroDbContext(DbContextOptions<AeroDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AeroDbContext).Assembly);
        
        // Configure global conventions
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Set table naming convention
            entityType.SetTableName(entityType.DisplayName());
            
            // Configure audit properties
            // ...
        }
    }
}
```

### Entity Configurations

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.CreatedOn).IsRequired();
        
        // PostgreSQL-specific: JSONB column
        builder.Property(p => p.Metadata).HasColumnType("jsonb");
        
        // Index
        builder.HasIndex(p => p.Name);
    }
}
```

## Setup

### Configuration

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=aero;Username=aero;Password=password"
  }
}

// Program.cs
builder.Services.AddDbContext<AeroDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("YourProject");
            npgsqlOptions.EnableRetryOnFailure(3);
        });
    
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Register repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), 
    typeof(GenericEntityFrameworkRepository<>));
```

### Migrations

```bash
# Add migration
dotnet ef migrations add InitialCreate --project src/YourProject

# Update database
dotnet ef database update --project src/YourProject

# Generate SQL script
dotnet ef migrations script --project src/YourProject
```

## Advanced Features

### PostgreSQL-Specific Features

#### JSONB Support

```csharp
public class Product : Entity
{
    public string Name { get; set; }
    public ProductMetadata Metadata { get; set; } // Stored as JSONB
}

public class ProductMetadata
{
    public List<string> Tags { get; set; }
    public Dictionary<string, string> Attributes { get; set; }
}

// Query JSONB
var products = await repository.FindAsync(p => 
    EF.Functions.JsonContains(p.Metadata, "{\"Tags\": [\"electronics\"]}"));
```

#### Full-Text Search

```csharp
// Configure GIN index
builder.HasIndex(p => p.SearchVector).IsTsVectorExpressionIndex("english");

// Search
var results = await Context.Products
    .Where(p => p.SearchVector.Matches("database"))
    .ToListAsync();
```

#### Arrays

```csharp
public class Order : Entity
{
    public string[] Tags { get; set; }
}

// Query arrays
var orders = await repository.FindAsync(o => o.Tags.Contains("urgent"));
```

### Stored Procedures

```csharp
public class EfStoredProcRepository : IStoredProcRepository
{
    private readonly DbContext _context;

    public async Task<IEnumerable<T>> ExecuteStoredProcAsync<T>(
        string procedureName, 
        params SqlParameter[] parameters)
    {
        var sql = $"CALL {procedureName}({string.Join(",", parameters.Select(p => $"@{p.ParameterName}"))})";
        return await _context.Set<T>().FromSqlRaw(sql, parameters).ToListAsync();
    }
}
```

### Raw SQL

```csharp
// Execute raw SQL
var products = await Context.Products
    .FromSqlRaw("SELECT * FROM Products WHERE Price > {0}", minPrice)
    .ToListAsync();

// Execute non-query
await Context.Database.ExecuteSqlRawAsync(
    "UPDATE Products SET Price = Price * 1.1 WHERE Category = {0}", category);
```

## Repository Customization

### Extending the Base Repository

```csharp
public class ProductRepository : GenericEntityFrameworkRepository<Product>
{
    private readonly AeroDbContext _aeroContext;

    public ProductRepository(AeroDbContext context, ILogger<ProductRepository> log) 
        : base(context, log)
    {
        _aeroContext = context;
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string categoryId)
    {
        return await _aeroContext.Products
            .Where(p => p.CategoryId == categoryId)
            .Include(p => p.Category)
            .ToListAsync();
    }

    public async Task BulkUpdatePricesAsync(decimal percentage)
    {
        await _aeroContext.Database.ExecuteSqlRawAsync(
            "UPDATE Products SET Price = Price * (1 + {0} / 100.0)", percentage);
    }
}
```

### Using Specifications Pattern

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
}

public class ActiveProductsSpecification : ISpecification<Product>
{
    public Expression<Func<Product, bool>> Criteria => p => p.IsActive;
    public List<Expression<Func<Product, object>>> Includes => 
        new() { p => p.Category };
}

public async Task<IEnumerable<T>> FindBySpecificationAsync(ISpecification<T> spec)
{
    var query = DbSet.AsQueryable();
    
    query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));
    
    return await query.Where(spec.Criteria).ToListAsync();
}
```

## Performance Optimization

### Query Optimization

```csharp
// Disable tracking for read-only queries
public override async Task<IEnumerable<T>> GetAllAsync()
{
    return await DbSet.AsNoTracking().ToListAsync();
}

// Split queries for complex includes
var orders = await Context.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items)
    .AsSplitQuery()
    .ToListAsync();

// Compiled queries
private static readonly Func<AeroDbContext, string, Task<Product>> GetProductById =
    EF.CompileAsyncQuery((AeroDbContext context, string id) =>
        context.Products.FirstOrDefault(p => p.Id == id));
```

### Connection Resilience

```csharp
builder.Services.AddDbContext<AeroDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    });
});
```

## Best Practices

1. **Use Migrations** - Never modify database schema manually
2. **Index Strategically** - Add indexes based on query patterns
3. **Eager Loading** - Use Include for related data to avoid N+1
4. **NoTracking for Reads** - Improves performance for read-only scenarios
5. **Raw SQL Sparingly** - Use only when LINQ is insufficient
6. **Batch Operations** - Use ExecuteSqlRaw for bulk operations

## Related Packages

- `Aero.Persistence.Core` - Repository interfaces
- `Aero.Persistence` - Base repository implementations
- `Aero.Caching` - Caching decorators
