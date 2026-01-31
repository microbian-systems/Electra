using Electra.Web.BlogEngine.Data;
using Electra.Web.BlogEngine.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Electra.Web.BlogEngine.Extensions;

/// <summary>
/// Extension methods for registering blog services
/// </summary>
public static class BlogServiceCollectionExtensions
{
    /// <summary>
    /// Adds blog services to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">Database connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlogServices(this IServiceCollection services, string connectionString)
    {
        // Register DbContext
        services.AddDbContext<BlogDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();
        });

        // Register blog service
        services.AddScoped<IBlogService, BlogService>();

        return services;
    }

    /// <summary>
    /// Adds blog services with custom DbContext configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureDbContext">DbContext configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlogServices(this IServiceCollection services, 
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        // Register DbContext with custom configuration
        services.AddDbContext<BlogDbContext>(configureDbContext);

        // Register blog service
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IBlogRepository, BlogRepository>();

        return services;
    }

    /// <summary>
    /// Adds blog services with SQL Server
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">SQL Server connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlogServicesWithSqlServer(this IServiceCollection services, string connectionString)
    {
        return services.AddBlogServices(options =>
        {
            options.UseSqlServer(connectionString);
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();
        });
    }

    /// <summary>
    /// Adds blog services with PostgreSQL
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlogServicesWithPostgreSQL(this IServiceCollection services, string connectionString)
    {
        return services.AddBlogServices(options =>
        {
            options.UseNpgsql(connectionString);
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching();
        });
    }

    /// <summary>
    /// Adds blog services with in-memory database (for testing)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="databaseName">In-memory database name</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlogServicesInMemory(this IServiceCollection services, string databaseName = "BlogTestDb")
    {
        return services.AddBlogServices(options =>
        {
            options.UseInMemoryDatabase(databaseName);
            options.EnableSensitiveDataLogging(true);
        });
    }
}