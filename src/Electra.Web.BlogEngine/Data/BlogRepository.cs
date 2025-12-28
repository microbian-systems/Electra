using Electra.Web.BlogEngine.Models;
using Electra.Web.BlogEngine.Entities;
using LanguageExt;

namespace Electra.Web.BlogEngine.Data;


// todo - add cancellation token support in IBlogRepository interface methods
public interface IBlogRepository
{
    Task<IEnumerable<BlogEntry>> GetLatestBlogsAsync(int count);
    Task<PagedResult<BlogEntry>> GetPaginatedBlogsAsync(int pageNumber=1, int pageSize=10, bool publishedOnly = true);
    Task<Option<BlogEntry>> GetBlogByIdAsync(string id);
    Task<Option<BlogEntry>> GetBlogBySlugAsync(string slug);
    Task<IEnumerable<BlogEntry>> GetFeaturedBlogsAsync(int count = 5);
    Task<PagedResult<BlogEntry>> SearchBlogsAsync(string searchTerm, int pageNumber=1, int pageSize=10);
    Task<PagedResult<BlogEntry>> GetBlogsByTagAsync(string tag, int pageNumber=1, int pageSize=10);
    Task<Option<BlogEntry>> AddBlogAsync(BlogEntry blog);
    Task<Option<BlogEntry>> UpdateBlogAsync(BlogEntry? blog);
    Task<bool> DeleteBlogAsync(string id);
    Task<int> IncrementViewCountAsync(string id);
    Task<IEnumerable<string>> GetAllTagsAsync();
    Task<PagedResult<BlogEntry>> GetBlogsByAuthorAsync(string author, int pageNumber=1, int pageSize=10);
    Task<bool> BulkImportMarkdown(IReadOnlyList<MarkDownContentModel> blogs);
}