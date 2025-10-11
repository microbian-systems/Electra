using Electra.Core;
using Electra.Web.BlogEngine.Data;
using Electra.Web.BlogEngine.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Electra.Web.BlogEngine.Services;

/// <summary>
/// Service for managing blog operations
/// </summary>
public class BlogService(IBlogRepository repository, ILogger<BlogService> logger) : IBlogService
{
    public async Task<IEnumerable<Entities.BlogEntry>> GetLatestBlogsAsync(int count) => await repository.GetLatestBlogsAsync(count);

    public async Task<PaginatedResult<Entities.BlogEntry>> GetPaginatedBlogsAsync(int pageNumber, int pageSize, bool publishedOnly = true) => await repository.GetPaginatedBlogsAsync(pageNumber, pageSize, publishedOnly);

    public async Task<Entities.BlogEntry?> GetBlogByIdAsync(long id) => await repository.GetBlogByIdAsync(id);

    public async Task<Entities.BlogEntry?> GetBlogBySlugAsync(string slug) => await repository.GetBlogBySlugAsync(slug);

    public async Task<IEnumerable<Entities.BlogEntry>> GetFeaturedBlogsAsync(int count = 5) => await repository.GetFeaturedBlogsAsync(count);

    public async Task<PaginatedResult<Entities.BlogEntry>> SearchBlogsAsync(string searchTerm, int pageNumber, int pageSize) => await repository.SearchBlogsAsync(searchTerm, pageNumber, pageSize);

    public async Task<PaginatedResult<Entities.BlogEntry>> GetBlogsByTagAsync(string tag, int pageNumber, int pageSize) => await repository.GetBlogsByTagAsync(tag, pageNumber, pageSize);

    public async Task<Entities.BlogEntry> AddBlogAsync(Entities.BlogEntry blog) => await repository.AddBlogAsync(blog);

    public async Task<Entities.BlogEntry> UpdateBlogAsync(Entities.BlogEntry blog) => await repository.UpdateBlogAsync(blog);

    public async Task<bool> DeleteBlogAsync(long id) => await repository.DeleteBlogAsync(id);

    public async Task<string> GetContentAsHtmlAsync(Entities.BlogEntry blog) => await repository.GetContentAsHtmlAsync(blog);

    public async Task<string> GetRawMarkdownAsync(Entities.BlogEntry blog) => await repository.GetRawMarkdownAsync(blog);

    public async Task<int> IncrementViewCountAsync(long id) => await repository.IncrementViewCountAsync(id);

    public async Task<IEnumerable<string>> GetAllTagsAsync() => await repository.GetAllTagsAsync();

    public async Task<PaginatedResult<Entities.BlogEntry>> GetBlogsByAuthorAsync(string author, int pageNumber, int pageSize) => await repository.GetBlogsByAuthorAsync(author, pageNumber, pageSize);
}
