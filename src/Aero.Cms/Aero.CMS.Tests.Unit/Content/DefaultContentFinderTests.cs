using Aero.CMS.Core.Content.ContentFinders;
using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Shared.Interfaces;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class DefaultContentFinderTests
{
    private readonly IContentRepository _repo;
    private readonly ISystemClock _clock;
    private readonly DefaultContentFinder _finder;
    private readonly ContentFinderContext _context;
    private readonly DateTime _now = new(2026, 2, 19, 12, 0, 0, DateTimeKind.Utc);

    public DefaultContentFinderTests()
    {
        _repo = Substitute.For<IContentRepository>();
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(_now);
        _finder = new DefaultContentFinder(_repo, _clock);
        
        _context = new ContentFinderContext
        {
            Slug = "test-slug",
            HttpContext = Substitute.For<HttpContext>()
        };
    }

    [Fact]
    public async Task FindAsync_ReturnsDoc_WhenPublishedInPastWithNoExpiry()
    {
        // Arrange
        var doc = new ContentDocument 
        { 
            Slug = "test-slug", 
            Status = PublishingStatus.Published,
            PublishedAt = _now.AddDays(-1)
        };
        _repo.GetBySlugAsync("test-slug").Returns(doc);

        // Act
        var result = await _finder.FindAsync(_context);

        // Assert
        result.ShouldBe(doc);
    }

    [Fact]
    public async Task FindAsync_ReturnsNull_WhenDocNotFound()
    {
        // Arrange
        _repo.GetBySlugAsync("test-slug").Returns((ContentDocument?)null);

        // Act
        var result = await _finder.FindAsync(_context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_ReturnsNull_ForDraftInNonPreviewMode()
    {
        // Arrange
        var doc = new ContentDocument 
        { 
            Slug = "test-slug", 
            Status = PublishingStatus.Draft 
        };
        _repo.GetBySlugAsync("test-slug").Returns(doc);

        // Act
        var result = await _finder.FindAsync(_context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_ReturnsNull_WhenPublishedAtIsFuture()
    {
        // Arrange
        var doc = new ContentDocument 
        { 
            Slug = "test-slug", 
            Status = PublishingStatus.Published,
            PublishedAt = _now.AddDays(1)
        };
        _repo.GetBySlugAsync("test-slug").Returns(doc);

        // Act
        var result = await _finder.FindAsync(_context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_ReturnsNull_WhenExpiresAtIsPast()
    {
        // Arrange
        var doc = new ContentDocument 
        { 
            Slug = "test-slug", 
            Status = PublishingStatus.Published,
            PublishedAt = _now.AddDays(-2),
            ExpiresAt = _now.AddDays(-1)
        };
        _repo.GetBySlugAsync("test-slug").Returns(doc);

        // Act
        var result = await _finder.FindAsync(_context);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_ReturnsDoc_InPreviewRegardlessOfStatus()
    {
        // Arrange
        var doc = new ContentDocument 
        { 
            Slug = "test-slug", 
            Status = PublishingStatus.Draft 
        };
        _repo.GetBySlugAsync("test-slug").Returns(doc);

        var previewContext = new ContentFinderContext
        {
            Slug = "test-slug",
            HttpContext = Substitute.For<HttpContext>(),
            IsPreview = true
        };

        // Act
        var result = await _finder.FindAsync(previewContext);

        // Assert
        result.ShouldBe(doc);
    }
}
