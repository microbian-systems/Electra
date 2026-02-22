using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Site.Data;
using Aero.CMS.Core.Site.Models;
using Aero.CMS.Core.Site.Services;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Shared.Services;
using Aero.CMS.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
namespace Aero.CMS.Tests.Integration.Site;

public class SiteBootstrapServiceTests : RavenTestBase
{
    private readonly ISiteRepository _siteRepo;
    private readonly IContentTypeRepository _contentTypeRepo;
    private readonly IContentRepository _contentRepo;

    public SiteBootstrapServiceTests()
    {
        IndexCreation.CreateIndexes(typeof(SiteBootstrapServiceTests).Assembly, Store);
        var clock = new Core.Shared.Services.SystemClock();
        _siteRepo = new SiteRepository(Store, clock, NullLogger<SiteRepository>.Instance);
        _contentTypeRepo = new ContentTypeRepository(Store, clock, NullLogger<ContentTypeRepository>.Instance);
        
        var pipeline = new SaveHookPipeline<ContentDocument>(
            Enumerable.Empty<Aero.CMS.Core.Shared.Interfaces.IBeforeSaveHook<ContentDocument>>(),
            Enumerable.Empty<Aero.CMS.Core.Shared.Interfaces.IAfterSaveHook<ContentDocument>>());
        _contentRepo = new ContentRepository(Store, clock, NullLogger<ContentRepository>.Instance, pipeline);
    }

    private IServiceScopeFactory CreateScopeFactory()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISiteRepository)).Returns(_siteRepo);
        serviceProvider.GetService(typeof(IContentTypeRepository)).Returns(_contentTypeRepo);
        serviceProvider.GetService(typeof(IContentRepository)).Returns(_contentRepo);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        return scopeFactory;
    }

    [Fact]
    public async Task StartAsync_CreatesDefaultSiteDocument_WhenNoneExists()
    {
        var service = new SiteBootstrapService(CreateScopeFactory());
        
        await service.StartAsync(CancellationToken.None);
        WaitForIndexing(Store);
        
        var site = await _siteRepo.GetDefaultAsync();
        site.ShouldNotBeNull();
        site.Name.ShouldBe("My Aero Site");
        site.BaseUrl.ShouldBe("https://localhost:5001");
        site.Description.ShouldBe("Built with Aero CMS");
        site.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_CreatesPageContentType_WhenNoneExists()
    {
        var service = new SiteBootstrapService(CreateScopeFactory());
        
        await service.StartAsync(CancellationToken.None);
        WaitForIndexing(Store);
        
        var contentType = await _contentTypeRepo.GetByAliasAsync("page");
        contentType.ShouldNotBeNull();
        contentType.Name.ShouldBe("Page");
        contentType.Alias.ShouldBe("page");
        contentType.AllowAtRoot.ShouldBeTrue();
        contentType.RequiresApproval.ShouldBeFalse();
    }

    [Fact]
    public async Task StartAsync_DoesNotCreateDuplicateSite_WhenOneAlreadyExists()
    {
        var existingSite = new SiteDocument
        {
            Name = "Existing Site",
            IsDefault = true
        };
        await _siteRepo.SaveAsync(existingSite);
        WaitForIndexing(Store);
        
        var service = new SiteBootstrapService(CreateScopeFactory());
        await service.StartAsync(CancellationToken.None);
        WaitForIndexing(Store);
        
        var allSites = await _siteRepo.GetAllAsync();
        allSites.Count.ShouldBe(1);
        allSites[0].Name.ShouldBe("Existing Site");
    }

    [Fact]
    public async Task StartAsync_DoesNotCreateDuplicateContentType_WhenOneAlreadyExists()
    {
        var existingType = new Core.Content.Models.ContentTypeDocument
        {
            Name = "Existing Page",
            Alias = "page"
        };
        await _contentTypeRepo.SaveAsync(existingType);
        WaitForIndexing(Store);
        
        var service = new SiteBootstrapService(CreateScopeFactory());
        await service.StartAsync(CancellationToken.None);
        WaitForIndexing(Store);
        
        var contentType = await _contentTypeRepo.GetByAliasAsync("page");
        contentType.ShouldNotBeNull();
        contentType.Name.ShouldBe("Existing Page");
    }

    [Fact]
    public async Task AfterStartAsync_GetDefaultAsyncReturnsNonNull()
    {
        var service = new SiteBootstrapService(CreateScopeFactory());
        
        await service.StartAsync(CancellationToken.None);
        WaitForIndexing(Store);
        
        var site = await _siteRepo.GetDefaultAsync();
        site.ShouldNotBeNull();
    }

    [Fact]
    public async Task StopAsync_ReturnsCompletedTask()
    {
        var service = new SiteBootstrapService(CreateScopeFactory());
        
        await service.StopAsync(CancellationToken.None);
    }
}
