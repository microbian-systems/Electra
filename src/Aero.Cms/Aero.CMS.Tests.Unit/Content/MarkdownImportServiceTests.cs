using Aero.CMS.Core.Content.Data;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Content.Services;
using Aero.CMS.Core.Shared.Models;
using NSubstitute;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Aero.CMS.Tests.Unit.Content;

public class MarkdownImportServiceTests
{
    private readonly IContentRepository _contentRepository;
    private readonly MarkdownRendererService _markdownRenderer;
    private readonly MarkdownImportService _service;

    public MarkdownImportServiceTests()
    {
        _contentRepository = Substitute.For<IContentRepository>();
        _markdownRenderer = new MarkdownRendererService();
        _service = new MarkdownImportService(_contentRepository, _markdownRenderer);
        
        // Default setup for SaveAsync that returns success
        _contentRepository.SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(HandlerResult.Ok()));
    }

    [Fact]
    public async Task ImportAsync_ReturnsHandlerResultSuccess()
    {
        // Arrange
        var markdown = @"---
title: My Post
---
Body";

        // Act
        var result = await _service.ImportAsync(markdown);

        // Assert
        result.Success.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task ImportAsync_ContentTypeAliasIsBlogPost()
    {
        // Arrange
        var markdown = @"---
title: My Post
---
Body";
        ContentDocument savedDocument = null;
        _contentRepository.SaveAsync(Arg.Do<ContentDocument>(doc => savedDocument = doc), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(HandlerResult.Ok()));

        // Act
        await _service.ImportAsync(markdown);

        // Assert
        savedDocument.ShouldNotBeNull();
        savedDocument.ContentTypeAlias.ShouldBe("blogPost");
    }

    [Fact]
    public async Task ImportAsync_StatusIsDraft()
    {
        // Arrange
        var markdown = @"---
title: My Post
---
Body";
        ContentDocument savedDocument = null;
        _contentRepository.SaveAsync(Arg.Do<ContentDocument>(doc => savedDocument = doc), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(HandlerResult.Ok()));

        // Act
        await _service.ImportAsync(markdown);

        // Assert
        savedDocument.ShouldNotBeNull();
        savedDocument.Status.ShouldBe(PublishingStatus.Draft);
    }

    [Fact]
    public async Task ImportAsync_TitleFromFrontmatter()
    {
        // Arrange
        var markdown = @"---
title: My Title
---
Body";
        ContentDocument savedDocument = null;
        _contentRepository.SaveAsync(Arg.Do<ContentDocument>(doc => savedDocument = doc), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(HandlerResult.Ok()));

        // Act
        await _service.ImportAsync(markdown);

        // Assert
        savedDocument.ShouldNotBeNull();
        savedDocument.Name.ShouldBe("My Title");
    }

    [Fact]
    public async Task ImportAsync_SlugGeneratedFromTitleWhenAbsent()
    {
        // Arrange
        var markdown = @"---
title: Hello World
---
Body";
        ContentDocument savedDocument = null;
        _contentRepository.SaveAsync(Arg.Do<ContentDocument>(doc => savedDocument = doc), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(HandlerResult.Ok()));

        // Act
        await _service.ImportAsync(markdown);

        // Assert
        savedDocument.ShouldNotBeNull();
        savedDocument.Slug.ShouldBe("hello-world");
    }

    [Fact]
    public async Task ImportAsync_OneMarkdownBlockInBlocks()
    {
        // Arrange
        var markdown = @"---
title: Test
---
Body";
        ContentDocument savedDocument = null;
        _contentRepository.SaveAsync(Arg.Do<ContentDocument>(doc => savedDocument = doc), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(HandlerResult.Ok()));

        // Act
        await _service.ImportAsync(markdown);

        // Assert
        savedDocument.ShouldNotBeNull();
        savedDocument.Blocks.ShouldHaveSingleItem();
        savedDocument.Blocks[0].ShouldBeOfType<MarkdownBlock>();
        var block = (MarkdownBlock)savedDocument.Blocks[0];
        block.Markdown.ShouldBe("Body");
    }

    [Fact]
    public async Task ImportAsync_SaveAsyncCalledExactlyOnce()
    {
        // Arrange
        var markdown = @"---
title: Test
---
Body";

        // Act
        await _service.ImportAsync(markdown);

        // Assert
        await _contentRepository.Received(1).SaveAsync(Arg.Any<ContentDocument>(), Arg.Any<CancellationToken>());
    }
}