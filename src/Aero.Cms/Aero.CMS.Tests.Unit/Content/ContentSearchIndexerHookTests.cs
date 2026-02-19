using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Search;
using Aero.CMS.Core.Shared.Interfaces;
using NSubstitute;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class ContentSearchIndexerHookTests
{
    private readonly IBlockTreeTextExtractor _treeExtractor;
    private readonly ISystemClock _clock;
    private readonly ContentSearchIndexerHook _sut;
    private readonly DateTime _now = new(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);

    public ContentSearchIndexerHookTests()
    {
        _treeExtractor = Substitute.For<IBlockTreeTextExtractor>();
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(_now);
        
        _sut = new ContentSearchIndexerHook(_treeExtractor, _clock);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPopulateSearchTextAndMetadata()
    {
        // Arrange
        var doc = new ContentDocument 
        { 
            Name = "Doc Name",
            Properties = new Dictionary<string, object?> { ["pageTitle"] = "Custom Title" }
        };
        _treeExtractor.Extract(doc.Blocks).Returns("Extracted Text");

        // Act
        await _sut.ExecuteAsync(doc);

        // Assert
        doc.SearchText.ShouldBe("Extracted Text");
        doc.SearchMetadata.ShouldNotBeNull();
        doc.SearchMetadata.Title.ShouldBe("Custom Title");
        doc.SearchMetadata.LastIndexed.ShouldBe(_now);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFallbackToName_WhenPageTitleMissing()
    {
        // Arrange
        var doc = new ContentDocument { Name = "Doc Name" };

        // Act
        await _sut.ExecuteAsync(doc);

        // Assert
        doc.SearchMetadata.Title.ShouldBe("Doc Name");
    }

    [Fact]
    public void Priority_ShouldBe10()
    {
        _sut.Priority.ShouldBe(10);
    }
}
