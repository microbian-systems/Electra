using Aero.Cms.Areas.Cms.Controllers;
using Aero.Cms.Blocks;
using Aero.Cms.Indexes;
using Aero.Cms.Middleware;
using Aero.Cms.Models;
using Aero.Cms.Options;
using Aero.Cms.Routing;
using Aero.Cms.Services;
using Aero.Models.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace Aero.Cms;

public static class AeroCmsExtensions
{
    public static IServiceCollection AddAeroCms(this IServiceCollection services, Action<CmsOptions>? configureOptions = null)
    {
        var options = new CmsOptions();
        configureOptions?.Invoke(options);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        services.AddSingleton<IBlockRegistry, BlockRegistry>();
        services.AddScoped<ISiteResolver, SiteResolver>();
        services.AddScoped<IPageRouter, PageRouter>();
        services.AddScoped<IBlockRenderer, BlockRenderer>();
        services.AddScoped<ICmsContext, CmsContext>();
        services.AddScoped<CmsRouteTransformer>();
            
        return services;
    }

    public static IApplicationBuilder UseAeroCms(this IApplicationBuilder app)
    {
        app.UseMiddleware<SiteResolutionMiddleware>();
        app.UseMiddleware<PageRoutingMiddleware>();
        app.UseMiddleware<CmsOutputCachingMiddleware>();
            
        return app;
    }

    public static void MapAeroCms(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDynamicControllerRoute<CmsRouteTransformer>("{**slug}");
    }

    public static void EnsureCmsIndexes(this IDocumentStore store)
    {
        new Pages_BySiteAndUrl().Execute(store);
        new Sites_ByHostname().Execute(store);
    }
    
    public static async Task<IApplicationBuilder> SeedCmsDataAsync(this IApplicationBuilder app)
    {
        var scope = app.ApplicationServices.CreateScope();
        var sp = scope.ServiceProvider;
        var log = sp.GetRequiredService<ILogger<CmsController>>();
        
        // Initialize RavenDB CMS Indexes and Seed Data
        try
        {
            var documentStore = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
            log.LogInformation("Ensuring RavenDB CMS indexes are created...");
            documentStore.EnsureCmsIndexes();

            // Seed Roles
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Aero.Core.Identity.AeroRole>>();
            string[] roleNames = { 
                CmsRoles.Admin, 
                CmsRoles.Creator, 
                CmsRoles.Contributor, 
                CmsRoles.Viewer 
            };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    log.LogInformation("Seeding role: {RoleName}", roleName);
                    await roleManager.CreateAsync(new Aero.Core.Identity.AeroRole(roleName));
                }
            }

            // Seed Admin User
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AeroUser>>();
            var adminEmail = "admin@admin.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                log.LogInformation("Seeding default admin user: {AdminEmail}", adminEmail);
                adminUser = new AeroUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "CMS",
                    LastName = "Admin"
                };
                var result = await userManager.CreateAsync(adminUser, "*strongPassword1");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, CmsRoles.Admin);
                }
                else
                {
                    log.LogError("Failed to seed admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, CmsRoles.Admin))
                {
                    await userManager.AddToRoleAsync(adminUser, CmsRoles.Admin);
                }
            }

            using var session = documentStore.OpenAsyncSession();
            var siteCount = await Raven.Client.Documents.LinqExtensions.CountAsync(session.Query<SiteDocument>());
            SiteDocument? defaultSite = null;
            if (siteCount == 0)
            {
                log.LogInformation("Seeding default CMS site...");
                defaultSite = new SiteDocument
                {
                    Id = "sites/default",
                    Name = "Microbians.io",
                    Hostnames = new List<string> { "localhost", "127.0.0.1", "microbians.io" },
                    DefaultCulture = "en-US",
                    Theme = "default"
                };
                await session.StoreAsync(defaultSite);
            }
            else
            {
                defaultSite = await session.LoadAsync<SiteDocument>("sites/default");
            }

            if (defaultSite != null)
            {
                var testPage = await Raven.Client.Documents.LinqExtensions.FirstOrDefaultAsync(session.Query<PageDocument, Pages_BySiteAndUrl>()
                    .Where(x => x.SiteId == defaultSite.Id && x.FullUrl == "/cms-test"));

                if (testPage == null)
                {
                    log.LogInformation("Seeding CMS test page...");
                    var page = new PageDocument
                    {
                        Id = "pages/cms-test",
                        SiteId = defaultSite.Id,
                        FullUrl = "/cms-test",
                        Slug = "cms-test",
                        Template = "default",
                        Metadata = new PageMetadata
                        {
                            Title = "CMS Test Page",
                            SeoDescription = "A test page for the Aero CMS"
                        },
                        Blocks = new List<BlockDocument>
                        {
                            new BlockDocument
                            {
                                Type = "Hero",
                                Data = new Dictionary<string, object>
                                {
                                    { "Title", "Welcome to Aero CMS" },
                                    { "Subtitle", "This page is rendered dynamically from blocks." },
                                    { "CtaText", "Learn More" },
                                    { "CtaUrl", "#" }
                                }
                            },
                            new BlockDocument
                            {
                                Type = "RichText",
                                Data = new Dictionary<string, object>
                                {
                                    { "Content", "<h2>Core Features</h2><p>Aero CMS provides a flexible, block-based system for building dynamic web pages.</p>" }
                                }
                            }
                        },
                        PublishedState = PagePublishedState.Published,
                        LastModifiedUtc = DateTime.UtcNow
                    };
                    await session.StoreAsync(page);
                }
            }
            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "An error occurred while initializing RavenDB");
        }

        return app;
    }
}