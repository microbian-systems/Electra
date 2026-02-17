using Aero.Cms.Areas.Blog.Data;
using Aero.Cms.Areas.Blog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aero.Cms.Areas.Blog.Extensions;

/// <summary>
/// Extension methods for registering blog services
/// </summary>
public static class BlogServiceCollectionExtensions
{
    /// <summary>
    /// Adds blog services to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="config">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBlogServices(this IServiceCollection services)
    {
        // Register DbContext
        // services.AddDbContext<BlogDbContext>(options =>
        // {
        //     options.UseSqlite(connectionString);
        //     options.EnableSensitiveDataLogging(false);
        //     options.EnableServiceProviderCaching();
        // });

        // Register blog service
        //AddBlogServicesRavenDbInMemory(services, "AeroBlogDb");
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IBlogRepository, BlogRepositoryRaven>();

        return services;
    }
}