using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Extensions;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Models;
using NSubstitute;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class PageServiceTests
{
    private readonly IContentRepository _contentRepo = Substitute.For<IContentRepository>();
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private readonly PageService _service;

    public PageServiceTests()
    {
        _service = new PageService(_contentRepo, _clock);
    }

    [Fact]
    public async Task GetPagesForSiteAsync_ReturnsOnlyPagesMatchingSiteId()
    {
        var siteId = Guid.NewGuid();
        var otherSiteId = Guid.NewGuid();
        var pages = new List<ContentDocument>
        {
            new() { Name = "Page 1", Properties = { ["siteId"] = siteId.ToString() } },
            new() { Name = "Page 2", Properties = { ["siteId"] = otherSiteId.ToString() } },
            new() { Name = "Page 3", Properties = { ["siteId"] = siteId.ToString() } }
        };
        _contentRepo.GetByContentTypeAsync("page", Arg.Any<CancellationToken>())
            .Returns(pages);

        var result = await _service.GetPagesForSiteAsync(siteId);

        result.Count.ShouldBe(2);
        result.All(p => p.Properties["siteId"]?.ToString() == siteId.ToString()).ShouldBeTrue();
    }

    [Fact]
    public async Task GetPagesForSiteAsync_ReturnsEmptyList_WhenNoPagesForSite()
    {
        var siteId = Guid.NewGuid();
        _contentRepo.GetByContentTypeAsync("page", Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _service.GetPagesForSiteAsync(siteId);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPagesForSiteAsync_OrdersBySortOrderThenName()
    {
        var siteId = Guid.NewGuid();
        var pages = new List<ContentDocument>
        {
            new() { Name = "Zebra", SortOrder = 0, Properties = { ["siteId"] = siteId.ToString() } },
            new() { Name = "Alpha", SortOrder = 1, Properties = { ["siteId"] = siteId.ToString() } },
            new() { Name = "Beta", SortOrder = 0, Properties = { ["siteId"] = siteId.ToString() } }
        };
        _contentRepo.GetByContentTypeAsync("page", Arg.Any<CancellationToken>())
            .Returns(pages);

        var result = await _service.GetPagesForSiteAsync(siteId);

        result[0].Name.ShouldBe("Beta");
        result[1].Name.ShouldBe("Zebra");
        result[2].Name.ShouldBe("Alpha");
    }

    [Fact]
    public async Task CreatePageAsync_ReturnsFail_WhenNameIsEmpty()
    {
        var result = await _service.CreatePageAsync(Guid.NewGuid(), "", "/", "admin");

        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain("Page name is required.");
    }

    [Fact]
    public async Task CreatePageAsync_GeneratesSlugFromName_WhenSlugNotProvided()
    {
        var siteId = Guid.NewGuid();
        _contentRepo.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContentDocument?)null);
        _contentRepo.SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        var result = await _service.CreatePageAsync(siteId, "Hello World", "", "admin");

        result.Success.ShouldBeTrue();
        result.Value!.Slug.ShouldBe("/hello-world");
    }

    [Fact]
    public async Task CreatePageAsync_ReturnsFail_WhenSlugAlreadyExists()
    {
        var existing = new ContentDocument { Slug = "/about" };
        _contentRepo.GetBySlugAsync("/about", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _service.CreatePageAsync(Guid.NewGuid(), "About", "/about", "admin");

        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain("A page with slug '/about' already exists.");
    }

    [Fact]
    public async Task CreatePageAsync_SetsContentTypeAlias_ToPage()
    {
        _contentRepo.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContentDocument?)null);
        _contentRepo.SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        var result = await _service.CreatePageAsync(Guid.NewGuid(), "Test", "/test", "admin");

        result.Success.ShouldBeTrue();
        result.Value!.ContentTypeAlias.ShouldBe("page");
    }

    [Fact]
    public async Task CreatePageAsync_SetsStatus_ToPublished()
    {
        _contentRepo.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContentDocument?)null);
        _contentRepo.SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        var result = await _service.CreatePageAsync(Guid.NewGuid(), "Test", "/test", "admin");

        result.Success.ShouldBeTrue();
        result.Value!.Status.ShouldBe(PublishingStatus.Published);
    }

    [Fact]
    public async Task CreatePageAsync_SetsPublishedAt_FromISystemClock()
    {
        var expectedDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(expectedDate);
        _contentRepo.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContentDocument?)null);
        _contentRepo.SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        var result = await _service.CreatePageAsync(Guid.NewGuid(), "Test", "/test", "admin");

        result.Success.ShouldBeTrue();
        result.Value!.PublishedAt.ShouldBe(expectedDate);
    }

    [Fact]
    public async Task CreatePageAsync_PrependsSlashToSlug_IfNotPresent()
    {
        _contentRepo.GetBySlugAsync("/about-us", Arg.Any<CancellationToken>())
            .Returns((ContentDocument?)null);
        _contentRepo.SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        var result = await _service.CreatePageAsync(Guid.NewGuid(), "About Us", "about-us", "admin");

        result.Success.ShouldBeTrue();
        result.Value!.Slug.ShouldBe("/about-us");
    }

    [Fact]
    public async Task CreatePageAsync_StoresSiteId_InProperties()
    {
        var siteId = Guid.NewGuid();
        _contentRepo.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContentDocument?)null);
        _contentRepo.SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        var result = await _service.CreatePageAsync(siteId, "Test", "/test", "admin");

        result.Success.ShouldBeTrue();
        result.Value!.Properties["siteId"]?.ToString().ShouldBe(siteId.ToString());
    }

    [Fact]
    public async Task CreatePageAsync_CallsContentRepoSaveAsync_ExactlyOnce()
    {
        _contentRepo.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ContentDocument?)null);
        _contentRepo.SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        await _service.CreatePageAsync(Guid.NewGuid(), "Test", "/test", "admin");

        await _contentRepo.Received(1).SaveAsync(
            Arg.Any<ContentDocument>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavePageAsync_DelegatesToContentRepoSaveAsync()
    {
        var page = new ContentDocument { Name = "Test" };
        _contentRepo.SaveAsync(page, Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        await _service.SavePageAsync(page, "admin");

        await _contentRepo.Received(1).SaveAsync(page, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePageAsync_ReturnsFail_WhenPageNotFound()
    {
        _contentRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ContentDocument?)null);

        var result = await _service.DeletePageAsync(Guid.NewGuid(), "admin");

        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain("Page not found.");
    }

    [Fact]
    public async Task DeletePageAsync_CallsContentRepoDeleteAsync_WhenPageFound()
    {
        var pageId = Guid.NewGuid();
        var page = new ContentDocument { Name = "Test" };
        page.Id = pageId.ToString();
        _contentRepo.GetByIdAsync(pageId, Arg.Any<CancellationToken>())
            .Returns(page);
        _contentRepo.DeleteAsync(pageId, Arg.Any<CancellationToken>())
            .Returns(HandlerResult.Ok());

        var result = await _service.DeletePageAsync(pageId, "admin");

        result.Success.ShouldBeTrue();
        await _contentRepo.Received(1).DeleteAsync(pageId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_DelegatesToContentRepo()
    {
        var pageId = Guid.NewGuid();
        var page = new ContentDocument { Name = "Test" };
        page.Id = pageId.ToString();
        _contentRepo.GetByIdAsync(pageId, Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _service.GetByIdAsync(pageId);

        result.ShouldBe(page);
    }

    [Fact]
    public async Task GetBySlugAsync_DelegatesToContentRepo()
    {
        var page = new ContentDocument { Slug = "/test" };
        _contentRepo.GetBySlugAsync("/test", Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _service.GetBySlugAsync("/test");

        result.ShouldBe(page);
    }
}
