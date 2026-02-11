

using Microsoft.EntityFrameworkCore;

namespace Electra.Auth.Services;

/// <summary>
/// Adapter to provide IDbContextFactory&lt;DbContext&gt; from an existing DbContext instance.
/// Used for dependency injection compatibility.
/// </summary>
public class DbContextFactoryAdapter : IDbContextFactory<DbContext>
{
    private readonly DbContext _context;

    public DbContextFactoryAdapter(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public DbContext CreateDbContext()
    {
        return _context;
    }
}
