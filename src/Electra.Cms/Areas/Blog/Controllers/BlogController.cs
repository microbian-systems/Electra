using Electra.Web.BlogEngine.Entities;
using Electra.Web.BlogEngine.Models;
using Electra.Web.BlogEngine.Services;
using Electra.Web.Core.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Electra.Web.BlogEngine.Controllers;

[AllowAnonymous]
[Route("/blog")]
public class BlogController(IBlogService blogService, ILogger<ElectraWebBaseController> log) : ElectraWebBaseController(log)
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int page = 1)
    {
        if (page < 1) page = 1;
        int pageSize = 9;

        var pagedResult = await blogService.GetPaginatedBlogsAsync(page, pageSize);
        var featuredBlogs = await blogService.GetFeaturedBlogsAsync(1);
        var featured = featuredBlogs.FirstOrDefault();

        var viewModel = new BlogIndexViewModel
        {
            CurrentPage = pagedResult.PageNumber,
            TotalPages = pagedResult.TotalPages,
            PageSize = pagedResult.PageSize,
            FeaturedArticle = featured != null ? MapToViewModel(featured) : null,
            Articles = pagedResult.Items.Select(MapToViewModel).ToList(),
            MetaTitle = "Blog - Microbians.io",
            MetaDescription = "Read our latest thoughts, tutorials and updates."
        };

        return View(viewModel);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Article(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return BadRequest("slug was not provided");
        
        var blogOption = await blogService.GetBlogBySlugAsync(slug);

        if (blogOption.IsNone)
            return NotFound();

        var blog = blogOption.IfNone(new BlogPost()); // todo : handle better

        await blogService.IncrementViewCountAsync(blog.BlogId); 
        
        var viewModel = MapToViewModel(blog);
        viewModel.Content = await blogService.GetContentAsHtmlAsync(blog);
        
        // Get related articles 
        var related = await blogService.GetRelatedBlogsAsync(blog.BlogId, 3);
        viewModel.RelatedArticles = related
            .Select(MapToViewModel)
            .ToList();

        return View(viewModel);
    }

    private static ArticleViewModel MapToViewModel(BlogPost blog)
    {
        return new ArticleViewModel
        {
            Id = blog.BlogId, 
            Title = blog.Title,
            Slug = blog.Slug,
            Content = blog.Content, 
            Excerpt = blog.Description,
            Author = blog.Authors.FirstOrDefault() ?? "Unknown",
            PublishedDate = blog.PublishDate,
            ThumbnailUrl = blog.ImageUrl,
            Tags = blog.Tags,
            Categories = new string[] { }, 
            MetaTitle = blog.Title,
            MetaDescription = blog.Description
        };
    }
}