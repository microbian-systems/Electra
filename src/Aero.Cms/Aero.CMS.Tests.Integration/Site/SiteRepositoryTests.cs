using Aero.CMS.Core.Site.Data;
using Aero.CMS.Core.Site.Models;
using Aero.CMS.Tests.Integration.Infrastructure;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aero.CMS.Tests.Integration.Site;

public class SiteRepositoryTests : RavenTestBase
{
    public SiteRepositoryTests()
    {
        IndexCreation.CreateIndexes(typeof(SiteRepositoryTests).Assembly, Store);
    }

    private SiteRepository CreateRepo()
    {
        var clock = new Core.Shared.Services.SystemClock();
        return new SiteRepository(Store, clock, NullLogger<SiteRepository>.Instance);
    }

    [Fact]
    public async Task GetDefaultAsync_ReturnsSiteWhereIsDefaultTrue()
    {
        var repo = CreateRepo();
        
        var site1 = new SiteDocument { Name = "Site 1", IsDefault = false };
        var site2 = new SiteDocument { Name = "Default Site", IsDefault = true };
        
        await repo.SaveAsync(site1);
        await repo.SaveAsync(site2);
        
        WaitForIndexing(Store);
        
        var result = await repo.GetDefaultAsync();
        
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Default Site");
        result.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDefaultAsync_ReturnsNullWhenNoSitesExist()
    {
        var repo = CreateRepo();
        
        var result = await repo.GetDefaultAsync();
        
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSavedSites()
    {
        var repo = CreateRepo();
        
        var site1 = new SiteDocument { Name = "Site 1" };
        var site2 = new SiteDocument { Name = "Site 2" };
        var site3 = new SiteDocument { Name = "Site 3" };
        
        await repo.SaveAsync(site1);
        await repo.SaveAsync(site2);
        await repo.SaveAsync(site3);
        
        WaitForIndexing(Store);
        
        var result = await repo.GetAllAsync();
        
        result.Count.ShouldBe(3);
        result.Select(s => s.Name).ShouldContain("Site 1");
        result.Select(s => s.Name).ShouldContain("Site 2");
        result.Select(s => s.Name).ShouldContain("Site 3");
    }

    [Fact]
    public async Task SaveAsync_ThenGetDefaultAsync_RetrievesWithAllFieldsIntact()
    {
        var repo = CreateRepo();
        
        var site = new SiteDocument
        {
            Name = "Test Site",
            BaseUrl = "https://test.example.com",
            Description = "A test site",
            DefaultLayout = "CustomLayout",
            FooterText = "Copyright 2024",
            IsDefault = true
        };
        
        await repo.SaveAsync(site);
        WaitForIndexing(Store);
        
        var result = await repo.GetDefaultAsync();
        
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Site");
        result.BaseUrl.ShouldBe("https://test.example.com");
        result.Description.ShouldBe("A test site");
        result.DefaultLayout.ShouldBe("CustomLayout");
        result.FooterText.ShouldBe("Copyright 2024");
        result.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public async Task TwoSitesExist_GetDefaultAsync_ReturnsTheOneWithIsDefaultTrue()
    {
        var repo = CreateRepo();
        
        var site1 = new SiteDocument { Name = "Non-default", IsDefault = false };
        var site2 = new SiteDocument { Name = "Default Site", IsDefault = true };
        
        await repo.SaveAsync(site1);
        await repo.SaveAsync(site2);
        
        WaitForIndexing(Store);
        
        var result = await repo.GetDefaultAsync();
        
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Default Site");
    }
}
