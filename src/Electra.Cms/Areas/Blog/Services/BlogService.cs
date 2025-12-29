using Electra.Models;
using Electra.Web.BlogEngine.Data;
using Electra.Web.BlogEngine.Entities;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Markdig;

namespace Electra.Web.BlogEngine.Services;

/// <summary>
/// Service for managing blog operations
/// </summary>
public class BlogService(IBlogRepository repository, ILogger<BlogService> logger) : IBlogService
{
    public async Task<IEnumerable<BlogPost>> GetLatestBlogsAsync(int count) => await repository.GetLatestBlogsAsync(count);

    public async Task<PagedResult<BlogPost>> GetPaginatedBlogsAsync(int pageNumber, int pageSize, bool publishedOnly = true) => await repository.GetPaginatedBlogsAsync(pageNumber, pageSize, publishedOnly);

    public async Task<Option<BlogPost>> GetBlogByIdAsync(string id) => await repository.GetBlogByIdAsync(id);

    public async Task<Option<BlogPost>> GetBlogBySlugAsync(string slug) => await repository.GetBlogBySlugAsync(slug);

    public async Task<IEnumerable<BlogPost>> GetFeaturedBlogsAsync(int count = 5) => await repository.GetFeaturedBlogsAsync(count);

    public async Task<PagedResult<BlogPost>> SearchBlogsAsync(string searchTerm, int pageNumber, int pageSize) => await repository.SearchBlogsAsync(searchTerm, pageNumber, pageSize);

    public async Task<PagedResult<BlogPost>> GetBlogsByTagAsync(string tag, int pageNumber, int pageSize) => await repository.GetBlogsByTagAsync(tag, pageNumber, pageSize);

    public async Task<Option<BlogPost>> AddBlogAsync(BlogPost blog) => await repository.AddBlogAsync(blog);

    public async Task<Option<BlogPost>> UpdateBlogAsync(BlogPost blog) => await repository.UpdateBlogAsync(blog);

    public async Task<bool> DeleteBlogAsync(string id) => await repository.DeleteBlogAsync(id);
    
    public async Task<string> GetContentAsHtmlAsync(BlogPost blog)
    {
        if (blog.ContentType == Enums.ContentType.HTML)
        {
            return blog.Content;
        }
        
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
            
        return Markdown.ToHtml(blog.Content, pipeline);
    }


    public async Task<int> IncrementViewCountAsync(string id) => await repository.IncrementViewCountAsync(id);

    public async Task<IEnumerable<BlogPost>> GetRelatedBlogsAsync(string blogId, int count = 3) => await repository.GetRelatedBlogsAsync(blogId, count);

    public async Task<IEnumerable<string>> GetAllTagsAsync() => await repository.GetAllTagsAsync();

    public async Task<PagedResult<BlogPost>> GetBlogsByAuthorAsync(string author, int pageNumber, int pageSize) => await repository.GetBlogsByAuthorAsync(author, pageNumber, pageSize);

    public async Task<IEnumerable<BlogPost>> GetLatestEntries(int count)
    {
        return await repository.GetLatestBlogsAsync(count);
    }
}
