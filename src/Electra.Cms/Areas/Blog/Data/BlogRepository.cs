using Electra.Cms.Areas.Blog.Entities;
using Electra.Models;
using LanguageExt;

namespace Electra.Cms.Areas.Blog.Data;


// todo - add cancellation token support in IBlogRepository interface methods
public interface IBlogRepository
{
    Task<IEnumerable<BlogPost>> GetLatestBlogsAsync(int count);
    Task<PagedResult<BlogPost>> GetPaginatedBlogsAsync(int pageNumber=1, int pageSize=10, bool publishedOnly = true);
    Task<Option<BlogPost>> GetBlogByIdAsync(string id);
    Task<Option<BlogPost>> GetBlogBySlugAsync(string slug);
    Task<IEnumerable<BlogPost>> GetFeaturedBlogsAsync(int count = 5);
    Task<PagedResult<BlogPost>> SearchBlogsAsync(string searchTerm, int pageNumber=1, int pageSize=10);
    Task<PagedResult<BlogPost>> GetBlogsByTagAsync(string tag, int pageNumber=1, int pageSize=10);
    Task<Option<BlogPost>> AddBlogAsync(BlogPost blog);
    Task<Option<BlogPost>> UpdateBlogAsync(BlogPost? blog);
    Task<bool> DeleteBlogAsync(string id);
    Task<int> IncrementViewCountAsync(string id);
    Task<IEnumerable<string>> GetAllTagsAsync();
    Task<PagedResult<BlogPost>> GetBlogsByAuthorAsync(string author, int pageNumber=1, int pageSize=10);
    Task<IEnumerable<BlogPost>> GetRelatedBlogsAsync(string blogId, int count = 3);
    Task<bool> BulkImportMarkdown(IReadOnlyList<MarkDownContentModel> blogs);
}