using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Site.Data;
using Aero.CMS.Core.Site.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aero.CMS.Core.Site.Services;

public class SiteBootstrapService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var siteRepo = scope.ServiceProvider.GetRequiredService<ISiteRepository>();
        var contentTypeRepo = scope.ServiceProvider.GetRequiredService<IContentTypeRepository>();

        var existing = await siteRepo.GetDefaultAsync(ct);
        if (existing is not null) return;

        var site = new SiteDocument
        {
            Name = "My Aero Site",
            BaseUrl = "https://localhost:5001",
            Description = "Built with Aero CMS",
            IsDefault = true,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow
        };
        await siteRepo.SaveAsync(site, ct);

        var pageType = await contentTypeRepo.GetByAliasAsync("page", ct);
        if (pageType is not null) return;

        var pageContentType = new ContentTypeDocument
        {
            Name = "Page",
            Alias = "page",
            Description = "Standard page type",
            AllowAtRoot = true,
            RequiresApproval = false,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            Properties =
            [
                new ContentTypeProperty
                {
                    Name = "Title",
                    Alias = "title",
                    PropertyType = PropertyType.Text,
                    Required = true,
                    SortOrder = 0
                },
                new ContentTypeProperty
                {
                    Name = "Description",
                    Alias = "description",
                    PropertyType = PropertyType.TextArea,
                    Required = false,
                    SortOrder = 1
                }
            ]
        };
        await contentTypeRepo.SaveAsync(pageContentType, ct);

        // Seed "home" page if absent
        var pageRepo = scope.ServiceProvider.GetRequiredService<IContentRepository>();
        var homePage = await pageRepo.GetBySlugAsync("/", ct);
        if (homePage is null)
        {
            var siteDoc = await siteRepo.GetDefaultAsync(ct);
            var page = new ContentDocument
            {
                Name = "Home",
                Slug = "/",
                ContentTypeAlias = "page",
                Status = PublishingStatus.Published,
                PublishedAt = DateTime.UtcNow,
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            };
            
            page.Properties["siteId"] = siteDoc!.Id;
            page.Properties["title"] = "Welcome to Aero CMS";
            page.Properties["description"] = "This is your seeded home page.";
            
            // Add a welcome block
            page.Blocks.Add(new HeroBlock 
            { 
                Heading = "Welcome to your new site!", 
                Subtext = "Edit this page in the admin dashboard to get started.",
                SortOrder = 0 
            });

            await pageRepo.SaveAsync(page, ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
