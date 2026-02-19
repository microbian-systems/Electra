using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Services;
using Aero.CMS.Tests.Integration.Infrastructure;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Integration.Content;

public class ContentRepositoryTests : RavenTestBase
{
    private readonly ISystemClock _clock;
    private readonly ContentRepository _sut;
    private readonly DateTime _now = new(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);

    public ContentRepositoryTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(_now);
        
        var pipeline = new SaveHookPipeline<ContentDocument>(
            Array.Empty<IBeforeSaveHook<ContentDocument>>(),
            Array.Empty<IAfterSaveHook<ContentDocument>>());
            
        _sut = new ContentRepository(Store, _clock, pipeline);
    }

    [Fact]
    public async Task GetBySlugAsync_Returns_Correct_Document()
    {
        // Arrange
        var doc = new ContentDocument { Name = "Page", Slug = "test-page" };
        await _sut.SaveAsync(doc);

        // Act
        var retrieved = await _sut.GetBySlugAsync("test-page");

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Slug.ShouldBe("test-page");
        retrieved.Name.ShouldBe("Page");
    }

    [Fact]
    public async Task GetChildrenAsync_Returns_Ordered_Children()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var child1 = new ContentDocument { Name = "Child 1", ParentId = parentId, SortOrder = 10 };
        var child2 = new ContentDocument { Name = "Child 2", ParentId = parentId, SortOrder = 5 };
        var other = new ContentDocument { Name = "Other", ParentId = Guid.NewGuid() };

        await _sut.SaveAsync(child1);
        await _sut.SaveAsync(child2);
        await _sut.SaveAsync(other);

        // Act
        var children = await _sut.GetChildrenAsync(parentId);

        // Assert
        children.Count.ShouldBe(2);
        children[0].Name.ShouldBe("Child 2"); // SortOrder 5
        children[1].Name.ShouldBe("Child 1"); // SortOrder 10
    }

    [Fact]
    public async Task GetChildrenAsync_Filters_By_Status()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var pub = new ContentDocument { Name = "Pub", ParentId = parentId, Status = PublishingStatus.Published };
        var draft = new ContentDocument { Name = "Draft", ParentId = parentId, Status = PublishingStatus.Draft };

        await _sut.SaveAsync(pub);
        await _sut.SaveAsync(draft);

        // Act
        var publishedOnly = await _sut.GetChildrenAsync(parentId, PublishingStatus.Published);

        // Assert
        publishedOnly.Count.ShouldBe(1);
        publishedOnly[0].Name.ShouldBe("Pub");
    }

    [Fact]
    public async Task GetByContentTypeAsync_Returns_Only_Matching()
    {
        // Arrange
        var typeA = new ContentDocument { Name = "A", ContentTypeAlias = "typeA" };
        var typeB = new ContentDocument { Name = "B", ContentTypeAlias = "typeB" };

        await _sut.SaveAsync(typeA);
        await _sut.SaveAsync(typeB);

        // Act
        var results = await _sut.GetByContentTypeAsync("typeA");

        // Assert
        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("A");
    }

    [Fact]
    public async Task SaveAsync_RoundTrips_Polymorphic_Blocks()
    {
        // Arrange
        var doc = new ContentDocument 
        { 
            Name = "Complex Page", 
            Slug = "complex",
            Blocks = new List<ContentBlock>
            {
                new RichTextBlock { Html = "<p>Rich</p>", SortOrder = 1 },
                new MarkdownBlock { Markdown = "# Mark", SortOrder = 2 },
                new DivBlock 
                { 
                    CssClass = "container",
                    SortOrder = 3,
                    Children = new List<ContentBlock>
                    {
                        new RichTextBlock { Html = "<span>Nested</span>" }
                    }
                }
            }
        };

        var saveResult = await _sut.SaveAsync(doc);
        saveResult.Success.ShouldBeTrue(string.Join(", ", saveResult.Errors));

        // Act
        var retrieved = await _sut.GetByIdAsync(((IEntity<Guid>)doc).Id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Blocks.Count.ShouldBe(3);
        
        var b1 = retrieved.Blocks[0].ShouldBeOfType<RichTextBlock>();
        b1.Html.ShouldBe("<p>Rich</p>");
        b1.Type.ShouldBe("richTextBlock");

        var b2 = retrieved.Blocks[1].ShouldBeOfType<MarkdownBlock>();
        b2.Markdown.ShouldBe("# Mark");

        var b3 = retrieved.Blocks[2].ShouldBeOfType<DivBlock>();
        b3.CssClass.ShouldBe("container");
        b3.Children.Count.ShouldBe(1);
        b3.Children[0].ShouldBeOfType<RichTextBlock>().Html.ShouldBe("<span>Nested</span>");
    }
}
