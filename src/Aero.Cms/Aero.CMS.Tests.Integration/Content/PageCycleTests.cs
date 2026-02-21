using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Services;
using Aero.CMS.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Integration.Content;

public class PageCycleTests : RavenTestBase
{
    private readonly IContentRepository _contentRepo;
    private readonly IPageService _pageService;
    private readonly ISystemClock _clock;

    public PageCycleTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTime.UtcNow);
        
        var hookPipeline = new SaveHookPipeline<ContentDocument>(
            new List<IBeforeSaveHook<ContentDocument>>(),
            new List<IAfterSaveHook<ContentDocument>>());

        _contentRepo = new ContentRepository(Store, _clock, hookPipeline);
        _pageService = new PageService(_contentRepo, _clock);
    }

    [Fact]
    public async Task FullPageCycle_ShouldWorkCorrectly()
    {
        // 1. Create Page
        var siteId = Guid.NewGuid();
        var pageName = "Test Home";
        var pageSlug = "/test-home";
        
        var createResult = await _pageService.CreatePageAsync(siteId, pageName, pageSlug, "test-user");
        createResult.Success.ShouldBeTrue();
        var pageId = Guid.Parse(createResult.Value!.Id);

        // 2. Load and verify basic properties
        var loadedPage = await _pageService.GetBySlugAsync(pageSlug);
        loadedPage.ShouldNotBeNull();
        loadedPage!.Name.ShouldBe(pageName);
        loadedPage.Properties["siteId"].ToString().ShouldBe(siteId.ToString());

        // 3. Add Sections and Blocks
        var sectionSvc = new SectionService();
        var section = sectionSvc.AddSection(loadedPage!, SectionLayout.TwoColumn);
        
        var hero = new HeroBlock { Heading = "Welcome", Subtext = "To the test" };
        sectionSvc.AddBlock(loadedPage!, section.Id, 0, hero);
        
        var richText = new RichTextBlock { Html = "<p>Right column</p>" };
        sectionSvc.AddBlock(loadedPage!, section.Id, 1, richText);

        // 4. Save Page
        var saveResult = await _pageService.SavePageAsync(loadedPage!, "test-user");
        saveResult.Success.ShouldBeTrue();

        // 5. Reload and verify block tree
        var reloadedPage = await _pageService.GetBySlugAsync(pageSlug);
        reloadedPage.ShouldNotBeNull();
        reloadedPage!.Blocks.Count.ShouldBe(1);
        
        var reloadedSection = reloadedPage.Blocks[0].ShouldBeOfType<SectionBlock>();
        reloadedSection.Layout.ShouldBe(SectionLayout.TwoColumn);
        reloadedSection.Children.Count.ShouldBe(2);
        
        var leftCol = reloadedSection.Children[0].ShouldBeOfType<ColumnBlock>();
        leftCol.Children.Count.ShouldBe(1);
        var reloadedHero = leftCol.Children[0].ShouldBeOfType<HeroBlock>();
        reloadedHero.Heading.ShouldBe("Welcome");
        
        var rightCol = reloadedSection.Children[1].ShouldBeOfType<ColumnBlock>();
        rightCol.Children.Count.ShouldBe(1);
        var reloadedRichText = rightCol.Children[0].ShouldBeOfType<RichTextBlock>();
        reloadedRichText.Html.ShouldBe("<p>Right column</p>");

        // 6. Delete Page
        var deleteResult = await _pageService.DeletePageAsync(pageId, "test-user");
        deleteResult.Success.ShouldBeTrue();

        // 7. Verify deletion
        var finalLookup = await _pageService.GetBySlugAsync(pageSlug);
        finalLookup.ShouldBeNull();
    }
}
