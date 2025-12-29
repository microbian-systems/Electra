using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Electra.Models;
using Electra.Web.BlogEngine.Controllers;
using Electra.Web.BlogEngine.Entities;
using Electra.Web.BlogEngine.Models;
using Electra.Web.BlogEngine.Services;
using Electra.Web.Core.Controllers;
using FluentAssertions;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Electra.Web.BlogEngine.Tests.Controllers;

public class BlogControllerTests
{
    private readonly IBlogService _blogService;
    private readonly ILogger<ElectraWebBaseController> _logger;
    private readonly BlogController _controller;

    public BlogControllerTests()
    {
        _blogService = Substitute.For<IBlogService>();
        _logger = Substitute.For<ILogger<ElectraWebBaseController>>();
        _controller = new BlogController(_blogService, _logger);
    }

    [Fact]
    public async Task Index_ReturnsViewWithBlogIndexViewModel()
    {
        // Arrange
        var pagedResult = new PagedResult<BlogPost>
        {
            Items = new List<BlogPost> { new BlogPost { Title = "Test Blog", Slug = "test-blog", Authors = new[] { "Author" } } },
            PageNumber = 1,
            PageSize = 9,
            TotalCount = 1
        };
        _blogService.GetPaginatedBlogsAsync(1, 9).Returns(Task.FromResult(pagedResult));
        _blogService.GetFeaturedBlogsAsync(1).Returns(Task.FromResult((IEnumerable<BlogPost>)new List<BlogPost>()));

        // Action
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<BlogIndexViewModel>(viewResult.Model);
        model.Articles.Should().HaveCount(1);
        model.Articles.First().Title.Should().Be("Test Blog");
    }

    [Fact]
    public async Task Article_WithValidSlug_ReturnsViewWithArticleViewModel()
    {
        // Arrange
        var slug = "test-article";
        var blogPost = new BlogPost 
        { 
            BlogId = "1",
            Title = "Test Article", 
            Slug = slug, 
            Authors = new[] { "Author" },
            Content = "Content"
        };
        _blogService.GetBlogBySlugAsync(slug).Returns(Task.FromResult(Option<BlogPost>.Some(blogPost)));
        _blogService.GetContentAsHtmlAsync(blogPost).Returns(Task.FromResult("<p>Content</p>"));
        _blogService.GetLatestBlogsAsync(4).Returns(Task.FromResult((IEnumerable<BlogPost>)new List<BlogPost>()));

        // Action
        var result = await _controller.Article(slug);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ArticleViewModel>(viewResult.Model);
        model.Title.Should().Be("Test Article");
        model.Content.Should().Be("<p>Content</p>");
        await _blogService.Received(1).IncrementViewCountAsync(blogPost.BlogId);
    }

    [Fact]
    public async Task Article_WithInvalidSlug_ReturnsNotFound()
    {
        // Arrange
        var slug = "non-existent";
        _blogService.GetBlogBySlugAsync(slug).Returns(Task.FromResult(Option<BlogPost>.None));

        // Action
        var result = await _controller.Article(slug);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
