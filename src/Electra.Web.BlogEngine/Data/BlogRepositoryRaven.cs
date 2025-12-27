using Electra.Web.BlogEngine.Entities;
using Electra.Web.BlogEngine.Models;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace Electra.Web.BlogEngine.Data;

public class BlogRepositoryRaven(IDocumentStore store, ILogger<BlogRepositoryRaven> log) : IBlogRepository
{
    public async Task<IEnumerable<BlogEntry>> GetLatestBlogsAsync(int count)
    {
        throw new NotImplementedException();
    }

    public async Task<PaginatedResult<BlogEntry>> GetPaginatedBlogsAsync(int pageNumber, int pageSize, bool publishedOnly = true)
    {
        throw new NotImplementedException();
    }

    public async Task<BlogEntry?> GetBlogByIdAsync(long id)
    {
        throw new NotImplementedException();
    }

    public async Task<BlogEntry?> GetBlogBySlugAsync(string slug)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<BlogEntry>> GetFeaturedBlogsAsync(int count = 5)
    {
        throw new NotImplementedException();
    }

    public async Task<PaginatedResult<BlogEntry>> SearchBlogsAsync(string searchTerm, int pageNumber, int pageSize)
    {
        throw new NotImplementedException();
    }

    public async Task<PaginatedResult<BlogEntry>> GetBlogsByTagAsync(string tag, int pageNumber, int pageSize)
    {
        throw new NotImplementedException();
    }

    public async Task<BlogEntry> AddBlogAsync(BlogEntry blog)
    {
        throw new NotImplementedException();
    }

    public async Task<BlogEntry> UpdateBlogAsync(BlogEntry blog)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> DeleteBlogAsync(long id)
    {
        throw new NotImplementedException();
    }

    public async Task<int> IncrementViewCountAsync(long id)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<string>> GetAllTagsAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<PaginatedResult<BlogEntry>> GetBlogsByAuthorAsync(string author, int pageNumber, int pageSize)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetContentAsHtmlAsync(BlogEntry blog)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetRawMarkdownAsync(BlogEntry blog)
    {
        throw new NotImplementedException();
    }
}