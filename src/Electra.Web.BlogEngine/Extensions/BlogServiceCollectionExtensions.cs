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
    /// <param name="services">Service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds blog services to the dependency injection container
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <returns>Service collection for chaining</returns>
        public IServiceCollection AddBlogServices(string connectionString)
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
        /// <param name="configureDbContext">DbContext configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public IServiceCollection AddBlogServices(Action<DbContextOptionsBuilder> configureDbContext)
        {
            // Register DbContext with custom configuration
            services.AddDbContext<BlogDbContext>(configureDbContext);

            // Register blog service
            services.AddScoped<IBlogService, BlogService>();
            services.AddScoped<IBlogRepository, BlogRepositoryEfCore>();

            return services;
        }

        /// <summary>
        /// Adds blog services with SQL Server
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <returns>Service collection for chaining</returns>
        public IServiceCollection AddBlogServicesWithSqlServer(string connectionString)
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
        /// <param name="connectionString">PostgreSQL connection string</param>
        /// <returns>Service collection for chaining</returns>
        public IServiceCollection AddBlogServicesWithPostgreSQL(string connectionString)
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
        /// <param name="databaseName">In-memory database name</param>
        /// <returns>Service collection for chaining</returns>
        public IServiceCollection AddBlogServicesInMemory(string databaseName = "BlogTestDb")
        {
            return services.AddBlogServices(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.EnableSensitiveDataLogging(true);
            });
        }
    }
}