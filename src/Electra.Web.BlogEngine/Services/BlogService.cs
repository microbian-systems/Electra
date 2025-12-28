using Electra.Web.BlogEngine.Data;
using Electra.Web.BlogEngine.Entities;
using Electra.Web.BlogEngine.Models;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Electra.Web.BlogEngine.Services;

/// <summary>
/// Service for managing blog operations
/// </summary>
public class BlogService(IBlogRepository repository, ILogger<BlogService> logger) : IBlogService
{
    public async Task<IEnumerable<BlogEntry>> GetLatestBlogsAsync(int count) => await repository.GetLatestBlogsAsync(count);

    public async Task<PagedResult<BlogEntry>> GetPaginatedBlogsAsync(int pageNumber, int pageSize, bool publishedOnly = true) => await repository.GetPaginatedBlogsAsync(pageNumber, pageSize, publishedOnly);

    public async Task<Option<BlogEntry>> GetBlogByIdAsync(string id) => await repository.GetBlogByIdAsync(id);

    public async Task<Option<BlogEntry>> GetBlogBySlugAsync(string slug) => await repository.GetBlogBySlugAsync(slug);

    public async Task<IEnumerable<BlogEntry>> GetFeaturedBlogsAsync(int count = 5) => await repository.GetFeaturedBlogsAsync(count);

    public async Task<PagedResult<BlogEntry>> SearchBlogsAsync(string searchTerm, int pageNumber, int pageSize) => await repository.SearchBlogsAsync(searchTerm, pageNumber, pageSize);

    public async Task<PagedResult<BlogEntry>> GetBlogsByTagAsync(string tag, int pageNumber, int pageSize) => await repository.GetBlogsByTagAsync(tag, pageNumber, pageSize);

    public async Task<Option<BlogEntry>> AddBlogAsync(BlogEntry blog) => await repository.AddBlogAsync(blog);

    public async Task<Option<BlogEntry>> UpdateBlogAsync(BlogEntry blog) => await repository.UpdateBlogAsync(blog);

    public async Task<bool> DeleteBlogAsync(string id) => await repository.DeleteBlogAsync(id);
    public async Task<string> GetContentAsHtmlAsync(BlogEntry blog)
    {
        throw new NotImplementedException();
    }


    public async Task<int> IncrementViewCountAsync(string id) => await repository.IncrementViewCountAsync(id);

    public async Task<IEnumerable<string>> GetAllTagsAsync() => await repository.GetAllTagsAsync();

    public async Task<PagedResult<BlogEntry>> GetBlogsByAuthorAsync(string author, int pageNumber, int pageSize) => await repository.GetBlogsByAuthorAsync(author, pageNumber, pageSize);

    public async Task<IEnumerable<BlogEntry>> GetLatestEntries(int count)
    {
        return await repository.GetLatestBlogsAsync(count);
    }
}
