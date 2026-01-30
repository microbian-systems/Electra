using Electra.Web.BlogEngine.Data;
using Electra.Web.BlogEngine.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Embedded;

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
        //AddBlogServicesRavenDbInMemory(services, "ElectraBlogDb");
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IBlogRepository, BlogRepositoryRaven>();

        return services;
    }
}