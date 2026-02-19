using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class ContentFinderPipelineTests
{
    private readonly ContentFinderContext _context;

    public ContentFinderPipelineTests()
    {
        _context = new ContentFinderContext
        {
            Slug = "test-slug",
            HttpContext = Substitute.For<HttpContext>()
        };
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallFindersInPriorityOrder()
    {
        // Arrange
        var finder1 = Substitute.For<IContentFinder>();
        finder1.Priority.Returns(20);
        
        var expectedDoc = new ContentDocument { Name = "Found", Slug = "test-slug" };
        var finder2 = Substitute.For<IContentFinder>();
        finder2.Priority.Returns(10);
        finder2.FindAsync(_context).Returns(expectedDoc);
        
        var finder3 = Substitute.For<IContentFinder>();
        finder3.Priority.Returns(30);

        var pipeline = new ContentFinderPipeline(new[] { finder1, finder2, finder3 });

        // Act
        var result = await pipeline.ExecuteAsync(_context);

        // Assert - verify order by checking that since finder2 (priority 10) returns a value, 
        // the subsequent ones (finder1 at 20 and finder3 at 30) aren't called
        result.ShouldBe(expectedDoc);
        await finder2.Received(1).FindAsync(_context);
        await finder1.DidNotReceive().FindAsync(_context);
        await finder3.DidNotReceive().FindAsync(_context);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFirstNonNullResult()
    {
        // Arrange
        var finder1 = Substitute.For<IContentFinder>();
        finder1.Priority.Returns(10);
        finder1.FindAsync(_context).Returns((ContentDocument?)null);

        var expectedDoc = new ContentDocument { Name = "Found", Slug = "test-slug" };
        var finder2 = Substitute.For<IContentFinder>();
        finder2.Priority.Returns(20);
        finder2.FindAsync(_context).Returns(expectedDoc);

        var finder3 = Substitute.For<IContentFinder>();
        finder3.Priority.Returns(30);
        finder3.FindAsync(_context).Returns(new ContentDocument { Name = "Too Late", Slug = "test-slug" });

        var pipeline = new ContentFinderPipeline(new[] { finder1, finder2, finder3 });

        // Act
        var result = await pipeline.ExecuteAsync(_context);

        // Assert
        result.ShouldBe(expectedDoc);
        await finder3.DidNotReceive().FindAsync(_context);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNullWhenAllFindersReturnNull()
    {
        // Arrange
        var finder1 = Substitute.For<IContentFinder>();
        finder1.Priority.Returns(10);
        finder1.FindAsync(_context).Returns((ContentDocument?)null);

        var finder2 = Substitute.For<IContentFinder>();
        finder2.Priority.Returns(20);
        finder2.FindAsync(_context).Returns((ContentDocument?)null);

        var pipeline = new ContentFinderPipeline(new[] { finder1, finder2 });

        // Act
        var result = await pipeline.ExecuteAsync(_context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldStopAfterFirstSuccess()
    {
        // Arrange
        var finder1 = Substitute.For<IContentFinder>();
        finder1.Priority.Returns(10);
        finder1.FindAsync(_context).Returns(new ContentDocument { Name = "Found", Slug = "test-slug" });

        var finder2 = Substitute.For<IContentFinder>();
        finder2.Priority.Returns(20);

        var pipeline = new ContentFinderPipeline(new[] { finder1, finder2 });

        // Act
        await pipeline.ExecuteAsync(_context);

        // Assert
        await finder1.Received(1).FindAsync(_context);
        await finder2.DidNotReceive().FindAsync(_context);
    }
}
